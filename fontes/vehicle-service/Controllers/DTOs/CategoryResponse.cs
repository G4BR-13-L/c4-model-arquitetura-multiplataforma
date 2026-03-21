namespace VehicleService.API.Controllers.DTOs
{
    public sealed record CategoryResponse(Guid Id, string Name, string Description, List<string> Optionals);
}
