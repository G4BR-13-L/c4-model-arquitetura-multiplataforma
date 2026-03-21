namespace VehicleService.API.Models
{
    public sealed class Vehicle
    {
        public Guid Id { get; init; }
        public string Model { get; init; }
        public string LicensePlate { get; init; }
        public Guid CategoryId { get; init; }
        public Category Category { get; init; }
        public bool Available { get; internal set; }
        public decimal DailyPrice { get; set; }
        public DateTime CreatedAt { get; init; }
        public DateTime UpdatedAt { get; internal set; }

        public void Reserve()
        {
            if (!Available)
            {
                throw new InvalidOperationException("Veículo indisponível para reserva.");
            }

            Available = false;
            UpdatedAt = DateTime.Now;
        }

        public void Return()
        {
            Available = true;
            UpdatedAt = DateTime.Now;
        }
    }
}
