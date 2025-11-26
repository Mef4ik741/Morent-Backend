using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WebAPI.Domain.Models;

namespace WebAPI.Infrastructure.Data.Configurations;

public class CarConfig : IEntityTypeConfiguration<Car>
{
    public void Configure(EntityTypeBuilder<Car> builder)
    {
        builder.HasKey(c => c.Id);
        
        builder.Property(c => c.Name).IsRequired().HasMaxLength(100);
        builder.Property(c => c.Brand).IsRequired().HasMaxLength(50);
        builder.Property(c => c.Model).IsRequired().HasMaxLength(50);
        builder.Property(c => c.Category).IsRequired().HasMaxLength(50);
        builder.Property(c => c.Year).IsRequired();
        builder.Property(c => c.Price).IsRequired().HasColumnType("decimal(18,2)");
        builder.Property(c => c.Description).IsRequired().HasMaxLength(1000);
        
        // Настройка для хранения списка URL изображений
        // ИСПРАВЛЕННАЯ КОНФИГУРАЦИЯ
        builder.Property(c => c.ImageUrlsJson)
            .IsRequired()
            .HasDefaultValue("[]"); 
            // .HasColumnType("nvarchar(max)") -- УДАЛЕНА!
            
        // Игнорируем свойство ImageUrls, так как оно маппится из ImageUrlsJson
        builder.Ignore(c => c.ImageUrls);
        
        // Настройка для ImageUrl (главное изображение)
        builder.Property(c => c.ImageUrl).HasMaxLength(500);
        
        // Настройка для OwnerUserId
        builder.Property(c => c.OwnerUserId).HasMaxLength(450);
        
        // Настройка связи с CarBooking
        builder.HasMany(c => c.Bookings)
            .WithOne()
            .HasForeignKey(b => b.CarId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}