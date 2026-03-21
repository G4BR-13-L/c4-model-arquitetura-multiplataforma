namespace UserService.API.Infra.Messaging
{
    public sealed class LocalStackOptions
    {
        public const string SectionName = "LocalStack";

        public string ServiceUrl { get; init; } = "http://localhost:4566";
        public string Region { get; init; } = "us-east-1";
        public string AccessKey { get; init; } = "test";
        public string SecretKey { get; init; } = "test";
    }
}
