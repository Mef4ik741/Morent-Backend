using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WebAPI.Domain.Models;

namespace WebAPI.Infrastructure.Data.Configurations;

public class ReviewConfig : IEntityTypeConfiguration<Review>
{
    public void Configure(EntityTypeBuilder<Review> builder)
    {
        builder.HasKey(r => r.Id);

        builder.HasOne(r => r.User)
            .WithMany()
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.Reviewer)
            .WithMany()
            .HasForeignKey(r => r.ReviewerId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Property(r => r.Rating)
            .HasColumnType("decimal(2,1)")
            .IsRequired();

        builder.Property(r => r.Comment)
            .IsRequired(false)
            .HasMaxLength(1000);

        // Один отзыв на пользователя от одного рецензента
        builder.HasIndex(r => new { r.UserId, r.ReviewerId })
            .IsUnique();

        // Диапазон рейтинга 0..5
        builder.HasCheckConstraint("CK_Review_Rating_Range", "[Rating] >= 0 AND [Rating] <= 5");
    }
}
