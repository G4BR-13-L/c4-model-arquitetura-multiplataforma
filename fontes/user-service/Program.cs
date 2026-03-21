using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using UserService.API.Infra.Auth;
using UserService.API.Infra.Messaging;
using UserService.API.Infra.Notifications;
using UserService.API.Infra.Repositories;
using UserService.API.Models.KeyCloak;

namespace UserService.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Host.UseSerilog((context, services, configuration) => configuration
                    .ReadFrom.Configuration(context.Configuration)
                    .ReadFrom.Services(services)
                    .Enrich.FromLogContext()
                    .WriteTo.Console());

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
            builder.Services.AddSwaggerGen(options =>
            {
                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header
                });
                options.AddSecurityRequirement(new OpenApiSecurityRequirement
                    {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                    });
            });

            builder.Services
                .AddAuthentication(Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.Authority = builder.Configuration["Keycloak:Authority"];
                    options.Audience = builder.Configuration["Keycloak:Audience"];
                    options.RequireHttpsMetadata = bool.Parse(builder.Configuration["Keycloak:RequireHttpsMetadata"] ?? "true");
                });

            builder.Services.AddAuthorization();

            builder.Services.AddLocalStackMessaging(builder.Configuration);

            builder.Services.Configure<EmailNotificationOptions>(builder.Configuration.GetSection(EmailNotificationOptions.SectionName));
            builder.Services.AddScoped<IEmailNotificationService, EmailNotificationService>();

            builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(serviceName: builder.Configuration["OpenTelemetry:ServiceName"] ?? "user-service"))
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddEntityFrameworkCoreInstrumentation()
                .AddOtlpExporter(otlp =>
                {
                    otlp.Endpoint = new Uri(builder.Configuration["OpenTelemetry:OtlpEndpoint"] ?? "http://localhost:4318");
                    otlp.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.HttpProtobuf;
                }));

            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseAuthorization();
            app.MapControllers();
            app.Run();
        }
    }
}
