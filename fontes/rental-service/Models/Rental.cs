namespace Locacao.RentalService.Models
{
    public enum RentalStatus
    {
        Pendente,
        Confirmado,
        Cancelado,
        Concluido
    }

    public sealed class Rental
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string UsuarioId { get; set; }
        public string VeiculoId { get; set; }
        public DateOnly DataInicio { get; set; }
        public DateOnly DataFim { get; set; }
        public decimal ValorTotal { get; set; }
        public RentalStatus Status { get; set; } = RentalStatus.Pendente;
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? UpdatedAt { get; set; }

        public int TotalDias => DataFim.DayNumber - DataInicio.DayNumber;
    }
}
