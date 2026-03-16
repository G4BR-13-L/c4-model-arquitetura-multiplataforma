using Locacao.VehicleServiceApp.Models;

namespace Locacao.VehicleServiceApp.Models.Commands
{
    public sealed class CreateVehicleCommand
    {
        public string Placa { get; set; }
        public string Modelo { get; set; }
        public int Ano { get; set; }
        public string Categoria { get; set; }
        public decimal ValorDiaria { get; set; }
        public bool Disponivel { get; set; } = true;

        public Vehicle ToModel()
        {
            return new Vehicle
            {
                Placa = Placa,
                Modelo = Modelo,
                Ano = Ano,
                Categoria = Categoria,
                ValorDiaria = ValorDiaria,
                Disponivel = Disponivel
            };
        }
    }
}
