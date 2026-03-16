using Locacao.PaymentService.Models;

namespace Locacao.PaymentService.Infrastructure.Adapters
{
    public interface IPaymentGatewayAdapter
    {
        Task<bool> ProcessAsync(Payment payment);
        Task<bool> RefundAsync(Payment payment);
    }

    /// <summary>
    /// Adapter simulado para desenvolvimento local.
    /// Em produção substituir pela integração real com Stripe ou PagSeguro.
    /// </summary>
    public sealed class FakePaymentGatewayAdapter : IPaymentGatewayAdapter
    {
        private readonly ILogger<FakePaymentGatewayAdapter> _logger;

        public FakePaymentGatewayAdapter(ILogger<FakePaymentGatewayAdapter> logger)
        {
            _logger = logger;
        }

        public Task<bool> ProcessAsync(Payment payment)
        {
            _logger.LogInformation(
                "FakeGateway: processando pagamento {Id} de R$ {Valor} via {Metodo}",
                payment.Id, payment.Valor, payment.MetodoPagamento);

            // Simula aprovação para todos os pagamentos em dev
            // Para testar falha: retornar false quando Valor > 10000
            var approved = payment.Valor <= 10000;

            payment.GatewayTransactionId = approved
                ? $"FAKE-TXN-{Guid.NewGuid():N}"
                : null;

            return Task.FromResult(approved);
        }

        public Task<bool> RefundAsync(Payment payment)
        {
            _logger.LogInformation(
                "FakeGateway: reembolsando transação {TxId}",
                payment.GatewayTransactionId);

            return Task.FromResult(true);
        }
    }
}
