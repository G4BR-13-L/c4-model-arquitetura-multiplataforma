using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VehicleService.API.Controllers.DTOs;
using VehicleService.API.Data;

namespace VehicleService.API.Controllers
{
    [ApiController]
    [Route("vehicles")]
    public sealed class VehiclesController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<VehiclesController> _logger;

        public VehiclesController(AppDbContext context, ILogger<VehiclesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<VehicleResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll()
        {
            _logger.LogInformation("Buscando todos os veículos");

            var vehicles = await _context.Vehicles
                .AsNoTracking()
                .Select(v => new VehicleResponse(v.Id, v.Model, v.LicensePlate, v.CategoryId, v.Available, v.DailyPrice))
                .ToListAsync();

            _logger.LogInformation("{Count} veículo(s) retornado(s)", vehicles.Count);

            return Ok(vehicles);
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(VehicleResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(Guid id)
        {
            _logger.LogInformation("Buscando veículo com id {VehicleId}", id);

            var vehicle = await _context.Vehicles
                .AsNoTracking()
                .Where(v => v.Id == id)
                .Select(v => new VehicleResponse(v.Id, v.Model, v.LicensePlate, v.CategoryId, v.Available, v.DailyPrice))
                .FirstOrDefaultAsync();

            if (vehicle is null)
            {
                _logger.LogWarning("Veículo com id {VehicleId} não encontrado", id);
                return NotFound();
            }

            _logger.LogInformation("Veículo com id {VehicleId} encontrado", id);

            return Ok(vehicle);
        }

        [HttpPost("{id}/reservation")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Reserve(Guid id)
        {
            _logger.LogInformation("Iniciando reserva do veículo com id {VehicleId}", id);

            var vehicle = await _context.Vehicles.FindAsync(id);

            if (vehicle is null)
            {
                _logger.LogWarning("Veículo com id {VehicleId} não encontrado para reserva", id);
                return NotFound();
            }

            if (!vehicle.Available)
            {
                _logger.LogWarning("Veículo com id {VehicleId} não está disponível para reserva", id);
                return BadRequest("O veículo não está disponível para reserva.");
            }

            vehicle.Reserve();
            await _context.SaveChangesAsync();

            _logger.LogInformation("Veículo com id {VehicleId} reservado com sucesso", id);

            return NoContent();
        }

        [HttpPut("{id}/return")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Return(Guid id)
        {
            _logger.LogInformation("Iniciando devolução do veículo com id {VehicleId}", id);

            var vehicle = await _context.Vehicles.FindAsync(id);

            if (vehicle is null)
            {
                _logger.LogWarning("Veículo com id {VehicleId} não encontrado para devolução", id);
                return NotFound();
            }

            vehicle.Return();
            await _context.SaveChangesAsync();

            _logger.LogInformation("Veículo com id {VehicleId} devolvido com sucesso", id);

            return NoContent();
        }
    }
}

