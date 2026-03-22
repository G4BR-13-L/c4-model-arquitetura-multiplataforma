namespace VehicleService.API.Models.DTOs
{
    public sealed record VehicleReservedDto(Guid Id, string Model, string LicensePlate)
    {
        public static VehicleReservedDto Create(Vehicle vehicle)
            => new(vehicle.Id, vehicle.Model, vehicle.LicensePlate);
    }
}
