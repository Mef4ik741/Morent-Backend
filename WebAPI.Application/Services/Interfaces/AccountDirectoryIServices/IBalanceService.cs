using WebAPI.Application.DTOs;
using WebAPI.Domain.enums;

namespace WebAPI.Application.Services.Interfaces.AccountDirectoryIServices;

public interface IBalanceService
{
    Task<BalanceResponseDTO> GetUserBalanceAsync(string userId);
    Task<BalanceResponseDTO> TopUpBalanceAsync(string userId, TopUpBalanceRequestDTO request);
    Task<BalanceResponseDTO> DeductBalanceAsync(string userId, decimal amount, string description);
    Task<TransactionHistoryResponseDTO> GetTransactionHistoryAsync(string userId, int page = 1, int pageSize = 10);
    Task<bool> HasSufficientBalanceAsync(string userId, decimal amount);
    Task<BalanceResponseDTO> ProcessPaymentAsync(string userId, decimal amount, TransactionType type, string description, string? paymentMethod = null, string? transactionReference = null);
    
    (bool IsValid, string ErrorMessage) ValidateTopUpAmount(decimal amount);
    Task<(bool IsValid, string ErrorMessage)> ValidateDeductAmountAsync(string userId, decimal amount);
}
