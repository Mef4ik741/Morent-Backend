using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WebAPI.Domain.Models;

namespace WebAPI.Infrastructure.Data.Configurations;

public class RentNotificationConfig : IEntityTypeConfiguration<RentNotification>
{
    public void Configure(EntityTypeBuilder<RentNotification> builder)
    {
        builder.HasKey(rn => rn.Id);

        builder.Property(rn => rn.BookingId)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(rn => rn.CarId)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(rn => rn.RenterUserId)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(rn => rn.OwnerUserId)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(rn => rn.NotificationType)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(rn => rn.Message)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(rn => rn.IsRead)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(rn => rn.CreatedAt)
            .IsRequired()
            // ИСПРАВЛЕНИЕ: Замена MSSQL функции на PostgreSQL эквивалент
            .HasDefaultValueSql("NOW() AT TIME ZONE 'utc'");

        // Связи
        builder.HasOne(rn => rn.Booking)
            .WithMany()
            .HasForeignKey(rn => rn.BookingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(rn => rn.Car)
            .WithMany()
            .HasForeignKey(rn => rn.CarId)
            .OnDelete(DeleteBehavior.Restrict);

        // Индексы для производительности
        builder.HasIndex(rn => rn.OwnerUserId);
        builder.HasIndex(rn => rn.RenterUserId);
        builder.HasIndex(rn => rn.BookingId);
        builder.HasIndex(rn => new { rn.OwnerUserId, rn.IsRead });
        builder.HasIndex(rn => rn.CreatedAt);
    }
}