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
        builder.Property(r => r.Name).IsRequired();
        builder.Property(r => r.Name).HasMaxLength(30);
        
    }
}