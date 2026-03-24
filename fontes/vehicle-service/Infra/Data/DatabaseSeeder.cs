using Microsoft.EntityFrameworkCore;
using VehicleService.API.Data;
using VehicleService.API.Models;

namespace VehicleService.API.Infra.Data
{
    public static class DatabaseSeeder
    {
        public static async Task SeedAsync(AppDbContext context)
        {
            if (await context.Categories.AnyAsync())
                return;

            var hatchId = Guid.Parse("557dc64b-e7c4-40b0-918f-4469be2c5d06");
            var sedanId = Guid.Parse("a6f496ab-8436-4636-92f5-3452cf0bf748");
            var suvId   = Guid.Parse("d4e51d9d-d92a-4a5e-a066-ee8efdf5d101");

            var categories = new List<Category>
            {
                new()
                {
                    Id          = hatchId,
                    Name        = "Hatch",
                    Description = "Veículos compactos com porta traseira integrada ao vidro, ideais para uso urbano.",
                    Optionals   = ["Ar-condicionado", "Direção elétrica", "Central multimídia"]
                },
                new()
                {
                    Id          = sedanId,
                    Name        = "Sedan",
                    Description = "Veículos de médio porte com porta-malas separado, confortáveis para viagens.",
                    Optionals   = ["Ar-condicionado digital", "Banco de couro", "Sensor de estacionamento"]
                },
                new()
                {
                    Id          = suvId,
                    Name        = "SUV",
                    Description = "Veículos utilitários esportivos com tração elevada, ideais para estradas e terrenos variados.",
                    Optionals   = ["Teto solar", "Tração 4x4", "Câmera de ré", "Sistema de navegação"]
                }
            };

            var now = DateTime.Now;

            var vehicles = new List<Vehicle>
            {
                // Hatch
                new()
                {
                    Id           = Guid.NewGuid(),
                    Model        = "Volkswagen Gol",
                    LicensePlate = "ABC-1234",
                    CategoryId   = hatchId,
                    Available    = true,
                    DailyPrice   = 89.90m,
                    CreatedAt    = now,
                    UpdatedAt    = now
                },
                new()
                {
                    Id           = Guid.NewGuid(),
                    Model        = "Chevrolet Onix",
                    LicensePlate = "DEF-5678",
                    CategoryId   = hatchId,
                    Available    = true,
                    DailyPrice   = 95.00m,
                    CreatedAt    = now,
                    UpdatedAt    = now
                },
                new()
                {
                    Id           = Guid.NewGuid(),
                    Model        = "Ford Ka",
                    LicensePlate = "GHI-9012",
                    CategoryId   = hatchId,
                    Available    = true,
                    DailyPrice   = 85.50m,
                    CreatedAt    = now,
                    UpdatedAt    = now
                },
                // Sedan
                new()
                {
                    Id           = Guid.NewGuid(),
                    Model        = "Toyota Corolla",
                    LicensePlate = "JKL-3456",
                    CategoryId   = sedanId,
                    Available    = true,
                    DailyPrice   = 149.90m,
                    CreatedAt    = now,
                    UpdatedAt    = now
                },
                new()
                {
                    Id           = Guid.NewGuid(),
                    Model        = "Honda Civic",
                    LicensePlate = "MNO-7890",
                    CategoryId   = sedanId,
                    Available    = true,
                    DailyPrice   = 159.90m,
                    CreatedAt    = now,
                    UpdatedAt    = now
                },
                new()
                {
                    Id           = Guid.NewGuid(),
                    Model        = "Volkswagen Virtus",
                    LicensePlate = "PQR-1122",
                    CategoryId   = sedanId,
                    Available    = true,
                    DailyPrice   = 139.90m,
                    CreatedAt    = now,
                    UpdatedAt    = now
                },
                // SUV
                new()
                {
                    Id           = Guid.NewGuid(),
                    Model        = "Jeep Renegade",
                    LicensePlate = "STU-3344",
                    CategoryId   = suvId,
                    Available    = true,
                    DailyPrice   = 199.90m,
                    CreatedAt    = now,
                    UpdatedAt    = now
                },
                new()
                {
                    Id           = Guid.NewGuid(),
                    Model        = "Chevrolet Tracker",
                    LicensePlate = "VWX-5566",
                    CategoryId   = suvId,
                    Available    = true,
                    DailyPrice   = 219.90m,
                    CreatedAt    = now,
                    UpdatedAt    = now
                },
                new()
                {
                    Id           = Guid.NewGuid(),
                    Model        = "Hyundai Creta",
                    LicensePlate = "YZA-7788",
                    CategoryId   = suvId,
                    Available    = true,
                    DailyPrice   = 209.90m,
                    CreatedAt    = now,
                    UpdatedAt    = now
                }
            };

            await context.Categories.AddRangeAsync(categories);
            await context.Vehicles.AddRangeAsync(vehicles);
            await context.SaveChangesAsync();
        }
    }
}
