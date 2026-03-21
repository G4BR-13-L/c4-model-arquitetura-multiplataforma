using System.Text.Json.Serialization;

namespace VehicleService.API.Controllers.DTOs
{
    public sealed record VehicleResponse(
        [property: JsonPropertyName("id")] Guid Id,
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("license_plate")] string LicensePlate,
        [property: JsonPropertyName("category_id")] Guid CategoryId,
        [property: JsonPropertyName("available")] bool Available,
        [property: JsonPropertyName("daily_price")] decimal DailyPrice
    );
}
