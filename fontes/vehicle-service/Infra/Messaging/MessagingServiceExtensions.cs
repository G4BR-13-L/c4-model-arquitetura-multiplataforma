using Amazon;
using Amazon.Runtime;
using Amazon.SimpleNotificationService;
using Amazon.SQS;

namespace VehicleService.API.Infra.Messaging
{
    public static class MessagingServiceExtensions
    {
        public static IServiceCollection AddLocalStackMessaging(this IServiceCollection services, IConfiguration configuration)
        {
            var options = configuration.GetSection(LocalStackOptions.SectionName).Get<LocalStackOptions>()
                ?? new LocalStackOptions();

            var credentials = new BasicAWSCredentials(options.AccessKey, options.SecretKey);
            var region = RegionEndpoint.GetBySystemName(options.Region);

            services.AddSingleton<IAmazonSQS>(_ =>
                new AmazonSQSClient(credentials, new AmazonSQSConfig
                {
                    ServiceURL = options.ServiceUrl,
                    AuthenticationRegion = options.Region
                }));

            services.AddSingleton<IAmazonSimpleNotificationService>(_ =>
                new AmazonSimpleNotificationServiceClient(credentials, new AmazonSimpleNotificationServiceConfig
                {
                    ServiceURL = options.ServiceUrl,
                    AuthenticationRegion = options.Region
                }));

            services.AddScoped<IMessagePublisher, MessagePublisher>();            

            return services;
        }
    }
}
