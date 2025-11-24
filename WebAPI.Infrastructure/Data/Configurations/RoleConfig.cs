using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WebAPI.Domain.Models;

namespace WebAPI.Infrastructure.Data.Configurations;

public class RoleConfig : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.HasKey(r => r.Id);
        builder.ToTable("Roles");
        
        // Поле 'Name'
        builder.Property(r => r.Name).IsRequired().HasMaxLength(30);

        // --- ПОТЕНЦИАЛЬНОЕ ИСПРАВЛЕНИЕ ---
        // Если в вашей модели Role есть поле CreatedAt,
        // и оно должно иметь значение по умолчанию, используйте:
        /*
        builder.Property(r => r.CreatedAt)
            .IsRequired()
            // Используем исправленную функцию PostgreSQL
            .HasDefaultValueSql("NOW() AT TIME ZONE 'utc'"); 
        */
        // ---------------------------------
        
        // Связь с UserRoles (если есть)
        /*
        builder.HasMany(r => r.UserRoles)
            .WithOne(ur => ur.Role)
            .HasForeignKey(ur => ur.RoleId);
        */
    }
}