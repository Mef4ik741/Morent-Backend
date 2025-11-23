using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WebAPI.Domain.Models;

namespace WebAPI.Infrastructure.Data.Configurations;

public class ChatConversationConfig : IEntityTypeConfiguration<ChatConversation>
{
    public void Configure(EntityTypeBuilder<ChatConversation> builder)
    {
        builder.ToTable("ChatConversations");
            
        builder.HasKey(x => x.Id);
            
        builder.Property(x => x.User1Id)
            .IsRequired()
            .HasMaxLength(450);
                
        builder.Property(x => x.User2Id)
            .IsRequired()
            .HasMaxLength(450);
                
        builder.Property(x => x.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");
                
        builder.Property(x => x.LastMessageTime)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");
                
        builder.Property(x => x.IsActive)
            .HasDefaultValue(true);
                
        // Уникальный индекс для пары пользователей
        builder.HasIndex(x => new { x.User1Id, x.User2Id })
            .IsUnique()
            .HasDatabaseName("IX_ChatConversations_User1Id_User2Id");
                
        builder.HasIndex(x => x.LastMessageTime)
            .HasDatabaseName("IX_ChatConversations_LastMessageTime");
                
        // Связь с сообщениями
        builder.HasMany(x => x.Messages)
            .WithOne()
            .HasForeignKey(m => new { m.FromUserId, m.ToUserId })
            .HasPrincipalKey(c => new { c.User1Id, c.User2Id })
            .OnDelete(DeleteBehavior.Cascade);
    }
}


