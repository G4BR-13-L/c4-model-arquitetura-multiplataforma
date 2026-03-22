using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Amazon.SQS;
using Amazon.SQS.Model;
using System.Text.Json;

namespace VehicleService.API.Infra.Messaging
{
    public interface IMessagePublisher
    {
        Task PublishToQueueAsync<T>(string queueName, T message, CancellationToken cancellationToken = default);
        Task PublishToTopicAsync<T>(string topicArn, T message, CancellationToken cancellationToken = default);
    }

    public sealed class MessagePublisher : IMessagePublisher
    {
        private readonly IAmazonSQS _sqs;
        private readonly IAmazonSimpleNotificationService _sns;
        private readonly ILogger<MessagePublisher> _logger;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        };

        public MessagePublisher(
            IAmazonSQS sqs,
            IAmazonSimpleNotificationService sns,
            ILogger<MessagePublisher> logger)
        {
            _sqs = sqs;
            _sns = sns;
            _logger = logger;
        }

        public async Task PublishToQueueAsync<T>(string queueName, T message, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Publicando mensagem na fila SQS {QueueName}", queueName);

            var queueUrlResponse = await _sqs.GetQueueUrlAsync(queueName, cancellationToken);
            var body = JsonSerializer.Serialize(message, JsonOptions);

            var request = new SendMessageRequest
            {
                QueueUrl = queueUrlResponse.QueueUrl,
                MessageBody = body
            };

            var response = await _sqs.SendMessageAsync(request, cancellationToken);

            _logger.LogInformation(
                "Mensagem publicada na fila SQS {QueueName} com MessageId {MessageId}",
                queueName, response.MessageId);
        }

        public async Task PublishToTopicAsync<T>(string topicArn, T message, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Publicando mensagem no tópico SNS {TopicArn}", topicArn);

            var body = JsonSerializer.Serialize(message, JsonOptions);

            var request = new PublishRequest
            {
                TopicArn = topicArn,
                Message = body
            };

            var response = await _sns.PublishAsync(request, cancellationToken);

            _logger.LogInformation(
                "Mensagem publicada no tópico SNS {TopicArn} com MessageId {MessageId}",
                topicArn, response.MessageId);
        }
    }
}
