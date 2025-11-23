using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WebAPI.Domain.Models;

namespace WebAPI.Infrastructure.Data.Configurations;

public class BalanceTransactionConfig : IEntityTypeConfiguration<BalanceTransaction>
{
    public void Configure(EntityTypeBuilder<BalanceTransaction> builder)
    {
        builder.HasKey(bt => bt.Id);
        
        builder.Property(bt => bt.Id)
            .HasMaxLength(36)
            .IsRequired();
            
        builder.Property(bt => bt.UserId)
            .HasMaxLength(450)
            .IsRequired();
            
        builder.Property(bt => bt.Amount)
            .HasColumnType("decimal(18,2)")
            .IsRequired();
            
        builder.Property(bt => bt.Type)
            .IsRequired();
            
        builder.Property(bt => bt.Description)
            .HasMaxLength(500)
            .IsRequired();
            
        builder.Property(bt => bt.CreatedAt)
            .HasDefaultValueSql("GETUTCDATE()")
            .IsRequired();
            
        builder.Property(bt => bt.PaymentMethod)
            .HasMaxLength(100);
            
        builder.Property(bt => bt.TransactionReference)
            .HasMaxLength(200);
            
        // Связь с User
        builder.HasOne(bt => bt.User)
            .WithMany(u => u.BalanceTransactions)
            .HasForeignKey(bt => bt.UserId)
            .OnDelete(DeleteBehavior.Cascade);
            
        // Индексы
        builder.HasIndex(bt => bt.UserId);
        builder.HasIndex(bt => bt.CreatedAt);
        builder.HasIndex(bt => bt.Type);
    }
}
