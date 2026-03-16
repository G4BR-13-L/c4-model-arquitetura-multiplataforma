using Locacao.PaymentService.Models;

namespace Locacao.PaymentService.Infrastructure.Repositories
{
    public interface IPaymentRepository
    {
        Task<Payment?> GetByIdAsync(Guid id);
        Task CreateAsync(Payment payment);
        Task UpdateAsync(Payment payment);
    }

    public sealed class InMemoryPaymentRepository : IPaymentRepository
    {
        private readonly List<Payment> _payments = new();

        public Task<Payment?> GetByIdAsync(Guid id)
        {
            var payment = _payments.FirstOrDefault(p => p.Id == id);
            return Task.FromResult(payment);
        }

        public Task CreateAsync(Payment payment)
        {
            _payments.Add(payment);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Payment payment)
        {
            var index = _payments.FindIndex(p => p.Id == payment.Id);
            if (index >= 0)
                _payments[index] = payment;

            return Task.CompletedTask;
        }
    }
}
