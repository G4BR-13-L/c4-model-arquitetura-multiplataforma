using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Text.Json;
using VehicleService.API.Data;
using VehicleService.API.Infra.Notifications;
using VehicleService.API.Models.DTOs;

namespace VehicleService.API.Controllers
{
    [ApiController]
    [Route("v1/vehicles")]
    public sealed class VehiclesController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IEmailNotificationService _emailNotification;
        private readonly EmailNotificationOptions _emailOptions;
        private readonly ILogger<VehiclesController> _logger;

        public VehiclesController(
            AppDbContext context,
            IEmailNotificationService emailNotification,
            IOptions<EmailNotificationOptions> emailOptions,
            ILogger<VehiclesController> logger)
        {
            _context = context;
            _emailNotification = emailNotification;
            _emailOptions = emailOptions.Value;
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

        [Authorize]
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

            await _emailNotification.SendAsync(
                recipientEmail: "system@vehicle-service.com.br",
                recipientName: "System",
                subject: $"Veículo Reservado {vehicle.LicensePlate}",
                content: $"Veículo {vehicle.Model} com placa {vehicle.LicensePlate} reservado em {DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")}.",
                queueName: _emailOptions.EmailNotificationQueueName);

            return NoContent();
        }

        [Authorize]
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

            if (vehicle.Available)
            {
                _logger.LogWarning("Veículo com id {VehicleId} já está disponível para reserva", id);
                return BadRequest("Veículo já está disponível para reserva");
            }

            vehicle.Return();
            await _context.SaveChangesAsync();

            _logger.LogInformation("Veículo com id {VehicleId} devolvido com sucesso", id);

            await _emailNotification.SendAsync(
                recipientEmail: "system@vehicle-service.com.br",
                recipientName: "System",
                subject: $"Veículo Reservado {vehicle.LicensePlate}",
                content: $"Veículo {vehicle.Model} com placa {vehicle.LicensePlate} devolvido em {DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")}.",                
                queueName: _emailOptions.EmailNotificationQueueName);

            return NoContent();
        }
    }
}

