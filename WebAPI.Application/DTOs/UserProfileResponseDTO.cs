using WebAPI.Domain.enums;

namespace WebAPI.Application.DTOs;

public class UserProfileResponseDTO
{
    public string Id { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Surname { get; set; } = string.Empty;
    public string? ImageProfileURL { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsConfirmed { get; set; }
    public bool IsVerified { get; set; }
    public UserRank Rank { get; set; }
    public int ReviewCount { get; set; }
    public int NegativeReviewCount { get; set; }
    public decimal Balance { get; set; }
    public DateTime? LastAvatarUploadAt { get; set; }
    public List<string> Roles { get; set; } = new List<string>();
}
