using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VehicleService.API.Controllers.DTOs;
using VehicleService.API.Data;

namespace VehicleService.API.Controllers
{
    [ApiController]
    [Route("categories")]
    public sealed class CategoriesController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<CategoriesController> _logger;

        public CategoriesController(AppDbContext context, ILogger<CategoriesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<CategoryResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll()
        {
            _logger.LogInformation("Buscando todas as categorias");

            var categories = await _context.Categories
                .AsNoTracking()
                .Select(c => new CategoryResponse(c.Id, c.Name, c.Description, c.Optionals))
                .ToListAsync();

            _logger.LogInformation("{Count} categoria(s) retornada(s)", categories.Count);

            return Ok(categories);
        }

        [HttpGet("{id}/vehicles")]
        [ProducesResponseType(typeof(IEnumerable<VehicleResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetVehiclesByCategoryId(Guid id)
        {
            _logger.LogInformation("Buscando veículos da categoria com id {CategoryId}", id);

            var categoryExists = await _context.Categories
                .AsNoTracking()
                .AnyAsync(c => c.Id == id);

            if (!categoryExists)
            {
                _logger.LogWarning("Categoria com id {CategoryId} não encontrada", id);
                return NotFound();
            }

            var vehicles = await _context.Vehicles
                .AsNoTracking()
                .Where(v => v.CategoryId == id)
                .Select(v => new VehicleResponse(v.Id, v.Model, v.LicensePlate, v.CategoryId, v.Available, v.DailyPrice))
                .ToListAsync();

            _logger.LogInformation("{Count} veículo(s) retornado(s) para a categoria com id {CategoryId}", vehicles.Count, id);

            return Ok(vehicles);
        }
    }
}

