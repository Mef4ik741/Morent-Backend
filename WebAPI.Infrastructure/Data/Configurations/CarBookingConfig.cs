using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WebAPI.Domain.Models;

namespace WebAPI.Infrastructure.Data.Configurations;

public class CarBookingConfig : IEntityTypeConfiguration<CarBooking>
{
    public void Configure(EntityTypeBuilder<CarBooking> builder)
    {
        builder.HasKey(b => b.Id);
        builder.Property(b => b.Id).ValueGeneratedOnAdd();

        builder.Property(b => b.CarId).IsRequired();
        builder.Property(b => b.RenterUserId).IsRequired();
        builder.Property(b => b.StartDate).IsRequired();
        builder.Property(b => b.EndDate).IsRequired();
        builder.Property(b => b.TotalPrice).HasColumnType("decimal(18,2)");
        builder.Property(b => b.Agreement).IsRequired();
        builder.Property(b => b.StatusActive).IsRequired();
        builder.Property(b => b.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

        builder.HasIndex(b => new { b.CarId, b.StartDate, b.EndDate });

        // Настройка связи с Car
        builder.HasOne(b => b.Car)
            .WithMany()
            .HasForeignKey(b => b.CarId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
