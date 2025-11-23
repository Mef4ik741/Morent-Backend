using WebAPI.Domain.enums;

namespace WebAPI.Application.DTOs;

public class TopUpBalanceRequestDTO
{
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = "Card";
    public string? Description { get; set; }
}

public class BalanceResponseDTO
{
    public decimal Balance { get; set; }
    public string UserId { get; set; }
    public string Username { get; set; }
}

public class TransactionResponseDTO
{
    public string Id { get; set; }
    public decimal Amount { get; set; }
    public TransactionType Type { get; set; }
    public string Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? PaymentMethod { get; set; }
    public string? TransactionReference { get; set; }
}

public class TransactionHistoryResponseDTO
{
    public List<TransactionResponseDTO> Transactions { get; set; } = new();
    public decimal CurrentBalance { get; set; }
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}
