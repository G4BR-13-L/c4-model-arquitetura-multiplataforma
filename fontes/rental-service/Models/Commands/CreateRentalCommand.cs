using Locacao.RentalService.Models;

namespace Locacao.RentalService.Models.Commands
{
    public sealed class CreateRentalCommand
    {
        public string UsuarioId { get; set; }
        public string VeiculoId { get; set; }
        public DateOnly DataInicio { get; set; }
        public DateOnly DataFim { get; set; }
        public decimal ValorDiaria { get; set; }

        public Rental ToModel()
        {
            var totalDias = DataFim.DayNumber - DataInicio.DayNumber;

            return new Rental
            {
                UsuarioId = UsuarioId,
                VeiculoId = VeiculoId,
                DataInicio = DataInicio,
                DataFim = DataFim,
                ValorTotal = ValorDiaria * totalDias,
                Status = RentalStatus.Pendente
            };
        }
    }
}
