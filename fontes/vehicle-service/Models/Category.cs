namespace VehicleService.API.Models
{
    public sealed class Category
    {
        public Guid Id { get; init; }
        public string Name { get; init; }
        public string Description { get; init; }
        public List<string> Optionals { get; init; }
        public List<Vehicle> Vehicles { get; init; }
    }
}
