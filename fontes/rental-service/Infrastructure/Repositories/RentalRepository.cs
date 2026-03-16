using Locacao.RentalService.Models;

namespace Locacao.RentalService.Infrastructure.Repositories
{
    public interface IRentalRepository
    {
        Task<IEnumerable<Rental>> GetAllAsync(string? usuarioId = null);
        Task<Rental?> GetByIdAsync(Guid id);
        Task CreateAsync(Rental rental);
        Task UpdateAsync(Rental rental);
    }

    public sealed class InMemoryRentalRepository : IRentalRepository
    {
        private readonly List<Rental> _rentals = new();

        public Task<IEnumerable<Rental>> GetAllAsync(string? usuarioId = null)
        {
            var result = string.IsNullOrWhiteSpace(usuarioId)
                ? _rentals.AsEnumerable()
                : _rentals.Where(r => r.UsuarioId == usuarioId);

            return Task.FromResult(result);
        }

        public Task<Rental?> GetByIdAsync(Guid id)
        {
            var rental = _rentals.FirstOrDefault(r => r.Id == id);
            return Task.FromResult(rental);
        }

        public Task CreateAsync(Rental rental)
        {
            _rentals.Add(rental);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Rental rental)
        {
            var index = _rentals.FindIndex(r => r.Id == rental.Id);
            if (index >= 0)
                _rentals[index] = rental;

            return Task.CompletedTask;
        }
    }
}
