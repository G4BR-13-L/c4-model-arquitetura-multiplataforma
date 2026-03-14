using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using UserService.API.Infra;
using UserService.API.Infra.Persistence;
using UserService.API.Infra.Repositories;
using UserService.API.Models.KeyCloak;
using UserService.API.Services;

namespace UserService.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddOptions<KeyCloakSettings>()
                .BindConfiguration(nameof(KeyCloakSettings))
                .ValidateDataAnnotations()
                .ValidateOnStart();

            builder.Services.AddTransient<KeyCloakAdminAuthDelegatingHandler>();

            var connectionString = builder.Configuration.GetConnectionString("UserServiceDb")
                ?? throw new InvalidOperationException("Connection string 'UserServiceDb' was not found.");

            builder.Services.AddDbContext<UserDbContext>(options =>
                options.UseNpgsql(connectionString));

            builder.Services.AddHttpClient("KeyCloakAdmin", (sp, client) =>
            {
                var settings = sp.GetRequiredService<IOptions<KeyCloakSettings>>().Value;

                var baseUrl = settings.BaseUrl.TrimEnd('/');
                client.BaseAddress = new Uri($"{baseUrl}/admin/realms/{settings.Realm}/");
            })
                .AddHttpMessageHandler<KeyCloakAdminAuthDelegatingHandler>();

            builder.Services.AddHttpClient("KeyCloakAuth", (sp, client) =>
            {
                var settings = sp.GetRequiredService<IOptions<KeyCloakSettings>>().Value;

                var baseUrl = settings.BaseUrl.TrimEnd('/');
                client.BaseAddress = new Uri($"{baseUrl}/realms/{settings.Realm}/");
            });

            builder.Services.AddScoped<IKeyCloakAuthRepository, KeyCloakAuthRepository>();  
            builder.Services.AddScoped<IKeyCloakManagementRepository, KeyCloakManagementRepository>();
            builder.Services.AddScoped<IKeyCloakService, KeyCloakService>();
            builder.Services.AddScoped<IUserService, UserService.API.Services.UserService>();

            builder.Services.AddAuthorization();
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var keycloakSettings = builder.Configuration
                .GetSection(nameof(KeyCloakSettings))
                .Get<KeyCloakSettings>();

            builder.Services
                .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.Authority = $"{keycloakSettings.BaseUrl}/realms/{keycloakSettings.Realm}";
                    options.Audience = "account";
                    options.RequireHttpsMetadata = false;
                });

            var app = builder.Build();

            using (var scope = app.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<UserDbContext>();
                dbContext.Database.Migrate();
            }

            app.UseSwagger();
            app.UseSwaggerUI();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
