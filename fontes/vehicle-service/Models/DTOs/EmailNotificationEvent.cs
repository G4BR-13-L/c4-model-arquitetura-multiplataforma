using System.Text.Json.Serialization;

namespace VehicleService.API.Models.DTOs
{
    public sealed record EmailNotificationEvent(
        [property: JsonPropertyName("event_type")] string EventType,
        [property: JsonPropertyName("occurred_at")] DateTime OccurredAt,
        [property: JsonPropertyName("data")] EmailNotificationData Data
    );
    public sealed record EmailNotificationData(
        [property: JsonPropertyName("sender_email")] string SenderEmail,
        [property: JsonPropertyName("sender_name")] string SenderName,
        [property: JsonPropertyName("recipient_email")] string RecipientEmail,
        [property: JsonPropertyName("recipient_name")] string RecipientName,
        [property: JsonPropertyName("subject")] string Subject,
        [property: JsonPropertyName("content")] string Content
    );
}
