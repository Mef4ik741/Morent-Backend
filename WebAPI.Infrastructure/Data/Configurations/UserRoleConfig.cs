using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WebAPI.Domain.Models;

namespace WebAPI.Infrastructure.Data.Configurations;

public class UserRoleConfig  : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        // Используем автогенерируемый ID как первичный ключ
        builder.HasKey(ur => ur.Id);
        builder.Property(ur => ur.Id).ValueGeneratedOnAdd();
        builder.ToTable("UserRoles");

        // Настройка полей
        builder.Property(ur => ur.RoleId).IsRequired();
        builder.Property(ur => ur.UserId).IsRequired(false);
        
        builder.Property(ur => ur.CreatedAt)
            .IsRequired()
            // ИСПРАВЛЕНИЕ: Замена GETUTCDATE() на PostgreSQL эквивалент
            .HasDefaultValueSql("NOW() AT TIME ZONE 'utc'");

        // Создаем уникальные индексы для комбинаций
        // ПРИМЕЧАНИЕ: HasFilter("[UserId] IS NOT NULL") - это тоже MSSQL-синтаксис.
        // Для PostgreSQL фильтр должен быть: .HasFilter("(\"UserId\" IS NOT NULL)")
        builder.HasIndex(ur => new { ur.RoleId, ur.UserId })
            .IsUnique()
            // ИСПРАВЛЕНИЕ: Синтаксис фильтра для PostgreSQL
            .HasFilter("(\"UserId\" IS NOT NULL)");

        // Связи
        builder.HasOne(ur => ur.Role)
            .WithMany(r => r.UserRoles)
            .HasForeignKey(ur => ur.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ur => ur.User)
            .WithMany(u => u.UserRoles)
            .HasForeignKey(ur => ur.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}