namespace Locacao.VehicleServiceApp.Models
{
    public sealed class Vehicle
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Placa { get; set; }
        public string Modelo { get; set; }
        public int Ano { get; set; }
        public string Categoria { get; set; }
        public decimal ValorDiaria { get; set; }
        public bool Disponivel { get; set; } = true;
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    }
}
