using System.ComponentModel.DataAnnotations;

namespace WebAPI.Domain.Models;

public class ChatConversation
{
    [Key]
    public int Id { get; set; }
        
    [Required]
    [MaxLength(450)]
    public string User1Id { get; set; } = string.Empty;
        
    [Required]
    [MaxLength(450)]
    public string User2Id { get; set; } = string.Empty;
        
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
    public DateTime LastMessageTime { get; set; } = DateTime.UtcNow;
        
    public bool IsActive { get; set; } = true;
        
    // Навигационные свойства
    public ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
}