using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WebAPI.Domain.Models;

namespace WebAPI.Infrastructure.Data.Configurations;

public class UserConfig : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(u => u.Id);
        builder.ToTable("Users");
        
        var id = builder.Property(u => u.Id);
        id.HasMaxLength(450);
        id.IsRequired();

        var name = builder.Property(u => u.Name);
        name.IsRequired();
        name.HasMaxLength(30);
        
        var username = builder.Property(u => u.Username);
        username.IsRequired();
        username.HasMaxLength(30);
        
        var surname = builder.Property(u => u.Surname);
        surname.IsRequired();
        surname.HasMaxLength(30);

        var email = builder.Property(u => u.Email);
        email.IsRequired();
        email.HasMaxLength(254);

        var password = builder.Property(u => u.Password);
        password.IsRequired();

        var createdAt = builder.Property(u => u.CreatedAt);
        createdAt.IsRequired();
        // ИСПРАВЛЕНИЕ: Замена MSSQL функции на PostgreSQL эквивалент
        createdAt.HasDefaultValueSql("NOW() AT TIME ZONE 'utc'");
        
        var balance = builder.Property(u => u.Balance);
        // Примечание: decimal(18,2) в PostgreSQL соответствует numeric(18,2)
        balance.HasColumnType("decimal(18,2)"); 
        balance.HasDefaultValue(0.00m);
        balance.IsRequired();
    }
}