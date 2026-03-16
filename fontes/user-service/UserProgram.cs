using Microsoft.Extensions.Options;
using UserService.API.Infra;
using UserService.API.Infra.Repositories;
using UserService.API.Models.KeyCloak;

namespace UserService.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddOptions<KeyCloakSettings>()
                .BindConfiguration(nameof(KeyCloakSettings))
                .ValidateDataAnnotations()
                .ValidateOnStart();

            builder.Services.AddTransient<KeyCloakAdminAuthDelegatingHandler>();

            builder.Services.AddHttpClient("KeyCloakAdmin", (sp, client) =>
            {
                var settings = sp.GetRequiredService<IOptions<KeyCloakSettings>>().Value;
                var baseUrl = settings.BaseUrl.TrimEnd('/');
                client.BaseAddress = new Uri($"{baseUrl}/admin/realms/{settings.Realm}/");
            }).AddHttpMessageHandler<KeyCloakAdminAuthDelegatingHandler>();

            builder.Services.AddHttpClient("KeyCloakAuth", (sp, client) =>
            {
                var settings = sp.GetRequiredService<IOptions<KeyCloakSettings>>().Value;
                var baseUrl = settings.BaseUrl.TrimEnd('/');
                client.BaseAddress = new Uri($"{baseUrl}/realms/{settings.Realm}/");
            });

            builder.Services.AddScoped<IKeyCloakAuthRepository, KeyCloakAuthRepository>();
            builder.Services.AddScoped<IKeyCloakManagementRepository, KeyCloakManagementRepository>();

            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            // REMOVIDO: app.UseHttpsRedirection() — causa redirect loops em Docker

            app.UseAuthorization();
            app.MapControllers();
            app.Run();
        }
    }
}
