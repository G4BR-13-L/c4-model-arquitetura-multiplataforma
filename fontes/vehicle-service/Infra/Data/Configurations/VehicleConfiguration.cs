using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VehicleService.API.Models;

namespace VehicleService.API.Data.Configurations
{
    public sealed class VehicleConfiguration : IEntityTypeConfiguration<Vehicle>
    {
        public void Configure(EntityTypeBuilder<Vehicle> builder)
        {
            builder.ToTable("vehicles");

            builder.HasKey(v => v.Id);

            builder.Property(v => v.Id)
                .HasColumnName("id")
                .ValueGeneratedNever();

            builder.Property(v => v.Model)
                .HasColumnName("model")
                .HasMaxLength(150)
                .IsRequired();

            builder.Property(v => v.LicensePlate)
                .HasColumnName("license_plate")
                .HasMaxLength(20)
                .IsRequired();

            builder.HasIndex(v => v.LicensePlate)
                .IsUnique();

            builder.Property(v => v.CategoryId)
                .HasColumnName("category_id")
                .IsRequired();

            builder.Property(v => v.Available)
                .HasColumnName("available")
                .IsRequired();

            builder.Property(v => v.DailyPrice)
                .HasColumnName("daily_price")
                .HasPrecision(18, 2)
                .IsRequired();

            builder.Property(v => v.CreatedAt)
                .HasColumnName("created_at")
                .HasColumnType("timestamp without time zone")
                .IsRequired();

            builder.Property(v => v.UpdatedAt)
                .HasColumnName("updated_at")
                .HasColumnType("timestamp without time zone")
                .IsRequired();

            builder.HasOne(v => v.Category)
                .WithMany(c => c.Vehicles)
                .HasForeignKey(v => v.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
