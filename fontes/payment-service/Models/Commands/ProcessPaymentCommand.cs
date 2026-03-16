using Locacao.PaymentService.Models;

namespace Locacao.PaymentService.Models.Commands
{
    public sealed class ProcessPaymentCommand
    {
        public string LocacaoId { get; set; }
        public decimal Valor { get; set; }
        public string MetodoPagamento { get; set; }
        public string? NumeroCartao { get; set; }
        public string? Validade { get; set; }
        public string? Cvv { get; set; }

        public Payment ToModel()
        {
            Enum.TryParse<MetodoPagamento>(MetodoPagamento, ignoreCase: true, out var metodo);

            return new Payment
            {
                LocacaoId = LocacaoId,
                Valor = Valor,
                MetodoPagamento = metodo,
                Status = PaymentStatus.Pendente
            };
        }
    }
}
