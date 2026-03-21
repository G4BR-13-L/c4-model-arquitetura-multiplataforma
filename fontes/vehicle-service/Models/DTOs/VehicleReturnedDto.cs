namespace VehicleService.API.Models.DTOs
{
    public sealed record VehicleReturnedDto(Guid Id, string Model, string LicensePlate)
    {
        public static VehicleReturnedDto Create(Vehicle vehicle)
            => new(vehicle.Id, vehicle.Model, vehicle.LicensePlate);
    }
}
