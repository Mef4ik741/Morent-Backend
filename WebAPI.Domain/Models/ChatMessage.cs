using System.ComponentModel.DataAnnotations;

namespace WebAPI.Domain.Models;

public class ChatMessage
{
    [Key]
    public int Id { get; set; }
        
    [Required]
    [MaxLength(450)]
    public string FromUserId { get; set; } = string.Empty;
        
    [Required]
    [MaxLength(450)]
    public string ToUserId { get; set; } = string.Empty;
        
    [Required]
    [MaxLength(2000)]
    public string Message { get; set; } = string.Empty;
        
    // Тип сообщения: "text", "voice", "image"
    [MaxLength(20)]
    public string MessageType { get; set; } = "text";
        
    // URL для голосовых сообщений или изображений
    [MaxLength(500)]
    public string? FileUrl { get; set; }
        
    [Required]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        
    public bool IsRead { get; set; } = false;
        
    public bool IsDeleted { get; set; } = false;
        
    // Пометка, что сообщение было отредактировано, и время редактирования
    public bool IsEdited { get; set; } = false;
    public DateTime? EditedAt { get; set; }
        
    // Навигационные свойства (если нужны)
    // public User FromUser { get; set; }
    // public User ToUser { get; set; }
}

