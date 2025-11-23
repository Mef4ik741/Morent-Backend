using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WebAPI.Domain.Models;

namespace WebAPI.Infrastructure.Data.Configurations;

public class ChatMessageConfig : IEntityTypeConfiguration<ChatMessage>
{
    public void Configure(EntityTypeBuilder<ChatMessage> builder)
    {
        builder.ToTable("ChatMessages");
            
        builder.HasKey(x => x.Id);
            
        builder.Property(x => x.FromUserId)
            .IsRequired()
            .HasMaxLength(450);
                
        builder.Property(x => x.ToUserId)
            .IsRequired()
            .HasMaxLength(450);
                
        builder.Property(x => x.Message)
            .IsRequired()
            .HasMaxLength(2000);
                
        builder.Property(x => x.MessageType)
            .HasMaxLength(20)
            .HasDefaultValue("text");
                
        builder.Property(x => x.FileUrl)
            .HasMaxLength(500);
                
        builder.Property(x => x.Timestamp)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");
                
        builder.Property(x => x.IsRead)
            .HasDefaultValue(false);
                
        builder.Property(x => x.IsDeleted)
            .HasDefaultValue(false);
                
        builder.HasIndex(x => x.FromUserId)
            .HasDatabaseName("IX_ChatMessages_FromUserId");
                
        builder.HasIndex(x => x.ToUserId)
            .HasDatabaseName("IX_ChatMessages_ToUserId");
                
        builder.HasIndex(x => x.Timestamp)
            .HasDatabaseName("IX_ChatMessages_Timestamp");
                
        builder.HasIndex(x => new { x.FromUserId, x.ToUserId })
            .HasDatabaseName("IX_ChatMessages_FromUserId_ToUserId");
    }
}
