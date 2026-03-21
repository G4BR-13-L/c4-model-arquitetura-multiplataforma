using Microsoft.EntityFrameworkCore;
using VehicleService.API.Data.Configurations;
using VehicleService.API.Models;

namespace VehicleService.API.Data
{
    public sealed class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Category> Categories => Set<Category>();
        public DbSet<Vehicle> Vehicles => Set<Vehicle>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfiguration(new CategoryConfiguration());
            modelBuilder.ApplyConfiguration(new VehicleConfiguration());

            base.OnModelCreating(modelBuilder);
        }
    }
}
