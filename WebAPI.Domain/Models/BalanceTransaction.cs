using WebAPI.Domain.enums;

namespace WebAPI.Domain.Models;

public class BalanceTransaction
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string UserId { get; set; }
    public decimal Amount { get; set; }
    public string Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? PaymentMethod { get; set; }
    public string? TransactionReference { get; set; }
    
    public User User { get; set; }
}