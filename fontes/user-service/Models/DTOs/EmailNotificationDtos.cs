namespace UserService.API.Models.DTOs
{
    public sealed record EmailNotificationEvent(
        string EventType,
        DateTime OccurredAt,
        EmailNotificationData Data
    );

    public sealed record EmailNotificationData(
        string SenderEmail,
        string SenderName,
        string RecipientEmail,
        string RecipientName,
        string Subject,
        string Content
    );
}
