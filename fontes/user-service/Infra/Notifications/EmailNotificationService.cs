using Microsoft.Extensions.Options;
using UserService.API.Infra.Messaging;
using UserService.API.Models.DTOs;

namespace UserService.API.Infra.Notifications
{
    public interface IEmailNotificationService
    {
        Task SendAsync(
            string recipientEmail,
            string recipientName,
            string subject,
            string content,
            string queueName,
            CancellationToken cancellationToken = default);
    }
    public sealed class EmailNotificationService : IEmailNotificationService
    {
        private readonly IMessagePublisher _publisher;
        private readonly EmailNotificationOptions _options;
        private readonly ILogger<EmailNotificationService> _logger;

        public EmailNotificationService(
            IMessagePublisher publisher,
            IOptions<EmailNotificationOptions> options,
            ILogger<EmailNotificationService> logger)
        {
            _publisher = publisher;
            _options = options.Value;
            _logger = logger;
        }

        public async Task SendAsync(
            string recipientEmail,
            string recipientName,
            string subject,
            string content,
            string queueName,
            CancellationToken cancellationToken = default)
        {
            var @event = new EmailNotificationEvent(
                EventType: "notification.email",
                OccurredAt: DateTime.UtcNow,
                Data: new EmailNotificationData(
                    SenderEmail: _options.SenderEmail,
                    SenderName: _options.SenderName,
                    RecipientEmail: recipientEmail,
                    RecipientName: recipientName,
                    Subject: subject,
                    Content: content
                )
            );

            _logger.LogInformation(
                "Enviando notificação de e-mail para {RecipientEmail} via fila {QueueName}",
                recipientEmail, queueName);

            await _publisher.PublishToQueueAsync(queueName, @event, cancellationToken);
        }
    }
}
