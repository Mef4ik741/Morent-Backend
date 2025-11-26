using WebAPI.Domain.enums;

namespace WebAPI.Domain.Models;

public class User
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Username { get; set; }
    public string Email { get; set; }
    public string Name { get; set; }
    public string Surname { get; set; }

    public string Password { get; set; }
    public bool IsConfirmed { get; set; } = false;
    
    public bool IsVerified { get; set; } = false; 
    public UserRank Rank { get; set; } = UserRank.Beginner;
    public int ReviewCount { get; set; } = 0;
    public int NegativeReviewCount { get; set; } = 0;

    public string? ImageProfileURL { get; set; } =
        "https://res.cloudinary.com/duygiwcsz/image/upload/v1764154785/Gemini_Generated_Image_7g894c7g894c7g89_dyrhej.png";
    public DateTime? LastAvatarUploadAt { get; set; }
    
    public decimal Balance { get; set; } = 0.00m;

    public ICollection<UserRole> UserRoles { get; set; }
    public ICollection<BalanceTransaction> BalanceTransactions { get; set; }
    public ICollection<RefreshToken> RefreshTokens { get; set; }
    public DateTime CreatedAt { get; set; }
}