using Locacao.RentalService.Models;
using Locacao.RentalService.Models.Commands;
using Locacao.RentalService.Infrastructure.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Locacao.RentalService.Controllers
{
    [Route("v1/[controller]")]
    [ApiController]
    public class RentalController : ControllerBase
    {
        private readonly IRentalRepository _rentalRepository;

        public RentalController(IRentalRepository rentalRepository)
        {
            _rentalRepository = rentalRepository;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllAsync([FromQuery] string? usuarioId)
        {
            var rentals = await _rentalRepository.GetAllAsync(usuarioId);
            return Ok(rentals);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetByIdAsync(Guid id)
        {
            var rental = await _rentalRepository.GetByIdAsync(id);
            if (rental is null)
                return NotFound(new { Message = $"Locação com id '{id}' não encontrada." });

            return Ok(rental);
        }

        [HttpPost]
        public async Task<IActionResult> CreateAsync([FromBody] CreateRentalCommand command)
        {
            var rental = command.ToModel();
            await _rentalRepository.CreateAsync(rental);
            return Created($"/v1/rental/{rental.Id}", rental);
        }

        [HttpPut("{id:guid}/cancelar")]
        public async Task<IActionResult> CancelAsync(Guid id)
        {
            var rental = await _rentalRepository.GetByIdAsync(id);
            if (rental is null)
                return NotFound(new { Message = $"Locação com id '{id}' não encontrada." });

            if (rental.Status == RentalStatus.Concluido)
                return BadRequest(new { Message = "Não é possível cancelar uma locação já concluída." });

            rental.Status = RentalStatus.Cancelado;
            rental.UpdatedAt = DateTimeOffset.UtcNow;
            await _rentalRepository.UpdateAsync(rental);

            return Ok(rental);
        }

        [HttpPut("{id:guid}/concluir")]
        public async Task<IActionResult> CompleteAsync(Guid id)
        {
            var rental = await _rentalRepository.GetByIdAsync(id);
            if (rental is null)
                return NotFound(new { Message = $"Locação com id '{id}' não encontrada." });

            if (rental.Status == RentalStatus.Cancelado)
                return BadRequest(new { Message = "Não é possível concluir uma locação cancelada." });

            rental.Status = RentalStatus.Concluido;
            rental.UpdatedAt = DateTimeOffset.UtcNow;
            await _rentalRepository.UpdateAsync(rental);

            return Ok(rental);
        }
    }
}
