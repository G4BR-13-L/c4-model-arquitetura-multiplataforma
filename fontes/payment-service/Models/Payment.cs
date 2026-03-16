namespace Locacao.PaymentService.Models
{
    public enum PaymentStatus
    {
        Pendente,
        Confirmado,
        Falhou,
        Reembolsado
    }

    public enum MetodoPagamento
    {
        CartaoCredito,
        CartaoDebito,
        Pix,
        Boleto
    }

    public sealed class Payment
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string LocacaoId { get; set; }
        public decimal Valor { get; set; }
        public MetodoPagamento MetodoPagamento { get; set; }
        public PaymentStatus Status { get; set; } = PaymentStatus.Pendente;
        public string? GatewayTransactionId { get; set; }
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? ProcessedAt { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
    }
}
