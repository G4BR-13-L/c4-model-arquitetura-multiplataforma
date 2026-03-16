using Locacao.VehicleServiceApp.Models;

namespace Locacao.VehicleServiceApp.Infrastructure.Repositories
{
    public interface IVehicleRepository
    {
        Task<IEnumerable<Vehicle>> GetAllAsync(bool? disponivel = null);
        Task<Vehicle?> GetByIdAsync(Guid id);
        Task CreateAsync(Vehicle vehicle);
        Task UpdateAsync(Vehicle vehicle);
        Task DeleteAsync(Guid id);
    }

    public sealed class InMemoryVehicleRepository : IVehicleRepository
    {
        private readonly List<Vehicle> _vehicles = new();

        public Task<IEnumerable<Vehicle>> GetAllAsync(bool? disponivel = null)
        {
            var result = disponivel.HasValue
                ? _vehicles.Where(v => v.Disponivel == disponivel.Value)
                : _vehicles.AsEnumerable();

            return Task.FromResult(result);
        }

        public Task<Vehicle?> GetByIdAsync(Guid id)
        {
            var vehicle = _vehicles.FirstOrDefault(v => v.Id == id);
            return Task.FromResult(vehicle);
        }

        public Task CreateAsync(Vehicle vehicle)
        {
            _vehicles.Add(vehicle);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Vehicle vehicle)
        {
            var index = _vehicles.FindIndex(v => v.Id == vehicle.Id);
            if (index >= 0)
                _vehicles[index] = vehicle;

            return Task.CompletedTask;
        }

        public Task DeleteAsync(Guid id)
        {
            var vehicle = _vehicles.FirstOrDefault(v => v.Id == id);
            if (vehicle is not null)
                _vehicles.Remove(vehicle);

            return Task.CompletedTask;
        }
    }
}
