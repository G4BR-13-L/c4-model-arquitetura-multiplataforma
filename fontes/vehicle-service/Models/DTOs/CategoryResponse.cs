namespace VehicleService.API.Models.DTOs
{
    public sealed record CategoryResponse(Guid Id, string Name, string Description, List<string> Optionals);
}
