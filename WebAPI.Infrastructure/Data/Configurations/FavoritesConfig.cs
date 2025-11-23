using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WebAPI.Domain.Models;

namespace WebAPI.Infrastructure.Data.Configurations;

public class FavoritesConfig : IEntityTypeConfiguration<Favorites>
{
    public void Configure(EntityTypeBuilder<Favorites> builder)
    {
        builder.ToTable("Favorites");

        builder.HasKey(f => f.Id);

        builder.Property(f => f.Id)
            .IsRequired();

        builder.Property(f => f.UserId)
            .IsRequired();

        // Составной уникальный индекс: одна и та же машина не может быть добавлена в избранное одним пользователем более одного раза
        builder.HasIndex(f => new { f.UserId, f.CarId })
            .IsUnique();

        builder.Property(f => f.CarId)
            .IsRequired();

        builder.HasOne(f => f.Car)
            .WithMany()
            .HasForeignKey(f => f.CarId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}