using Locacao.VehicleServiceApp.Models;
using Locacao.VehicleServiceApp.Models.Commands;
using Locacao.VehicleServiceApp.Infrastructure.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Locacao.VehicleServiceApp.Controllers
{
    [Route("v1/[controller]")]
    [ApiController]
    public class VehicleController : ControllerBase
    {
        private readonly IVehicleRepository _vehicleRepository;

        public VehicleController(IVehicleRepository vehicleRepository)
        {
            _vehicleRepository = vehicleRepository;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllAsync([FromQuery] bool? disponivel)
        {
            var vehicles = await _vehicleRepository.GetAllAsync(disponivel);
            return Ok(vehicles);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetByIdAsync(Guid id)
        {
            var vehicle = await _vehicleRepository.GetByIdAsync(id);
            if (vehicle is null)
                return NotFound(new { Message = $"Veículo com id '{id}' não encontrado." });

            return Ok(vehicle);
        }

        [HttpPost]
        public async Task<IActionResult> CreateAsync([FromBody] CreateVehicleCommand command)
        {
            var vehicle = command.ToModel();
            await _vehicleRepository.CreateAsync(vehicle);
            return Created($"/v1/vehicle/{vehicle.Id}", vehicle);
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> UpdateAsync(Guid id, [FromBody] UpdateVehicleCommand command)
        {
            var existing = await _vehicleRepository.GetByIdAsync(id);
            if (existing is null)
                return NotFound(new { Message = $"Veículo com id '{id}' não encontrado." });

            existing.Modelo = command.Modelo ?? existing.Modelo;
            existing.ValorDiaria = command.ValorDiaria ?? existing.ValorDiaria;
            existing.Disponivel = command.Disponivel ?? existing.Disponivel;
            existing.Categoria = command.Categoria ?? existing.Categoria;

            await _vehicleRepository.UpdateAsync(existing);
            return Ok(existing);
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteAsync(Guid id)
        {
            var existing = await _vehicleRepository.GetByIdAsync(id);
            if (existing is null)
                return NotFound(new { Message = $"Veículo com id '{id}' não encontrado." });

            await _vehicleRepository.DeleteAsync(id);
            return NoContent();
        }
    }
}
