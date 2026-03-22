namespace VehicleService.API.Infra.Notifications
{
    public sealed class EmailNotificationOptions
    {
        public const string SectionName = "EmailNotification";

        public string EmailNotificationQueueName { get; init; }
        public string SenderEmail { get; init; } = string.Empty;
        public string SenderName { get; init; } = string.Empty;
    }
}
