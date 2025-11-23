namespace WebAPI.Domain.Models;

public class Review
{
    public int Id { get; set; }
    public string UserId { get; set; } 
    public string ReviewerId { get; set; } 
    public decimal Rating { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; }
    public User Reviewer { get; set; }
}
