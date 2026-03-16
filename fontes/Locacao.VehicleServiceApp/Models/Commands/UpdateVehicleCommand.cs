namespace Locacao.VehicleServiceApp.Models.Commands
{
    public sealed class UpdateVehicleCommand
    {
        public string? Modelo { get; set; }
        public string? Categoria { get; set; }
        public decimal? ValorDiaria { get; set; }
        public bool? Disponivel { get; set; }
    }
}
