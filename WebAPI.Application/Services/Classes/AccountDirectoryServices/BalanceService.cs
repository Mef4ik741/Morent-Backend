using Microsoft.EntityFrameworkCore;
using WebAPI.Application.DTOs;
using WebAPI.Application.Services.Interfaces.AccountDirectoryIServices;
using WebAPI.Domain.enums;
using WebAPI.Domain.Models;
using WebAPI.Infrastructure.Data.Context;

namespace WebAPI.Application.Services.Classes.AccountDirectoryServices;

public class BalanceService : IBalanceService
{
    private readonly Context _context;

    public BalanceService(Context context)
    {
        _context = context;
    }

    public async Task<BalanceResponseDTO> GetUserBalanceAsync(string userId)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null) { throw new ArgumentException("Пользователь не найден"); }

        return new BalanceResponseDTO
        {
            Balance = user.Balance,
            UserId = user.Id,
            Username = user.Username
        };
    }

    public async Task<BalanceResponseDTO> TopUpBalanceAsync(string userId, TopUpBalanceRequestDTO request)
    {
        if (request.Amount <= 0){ throw new ArgumentException("Сумма пополнения должна быть больше нуля"); }
            
        if (request.Amount < 30){ throw new ArgumentException("Минимальная сумма пополнения: 30 долларов"); }

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null){ throw new ArgumentException("Пользователь не найден"); }
        
        var transaction = new BalanceTransaction
        {
            UserId = userId,
            Amount = request.Amount,
            Type = TransactionType.TopUp,
            Description = request.Description ?? $"Пополнение баланса на {request.Amount:C}",
            PaymentMethod = request.PaymentMethod,
            TransactionReference = Guid.NewGuid().ToString()
        };

        user.Balance += request.Amount;

        _context.BalanceTransactions.Add(transaction);
        await _context.SaveChangesAsync();

        return new BalanceResponseDTO
        {
            Balance = user.Balance,
            UserId = user.Id,
            Username = user.Username
        };
    }

    public async Task<BalanceResponseDTO> DeductBalanceAsync(string userId, decimal amount, string description)
    {
        if (amount <= 0)
        {
            throw new ArgumentException("Сумма списания должна быть больше нуля");
        }
        
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null) { throw new ArgumentException("Пользователь не найден"); }

        if (user.Balance < amount){ throw new InvalidOperationException("Недостаточно средств на балансе"); }

        var transaction = new BalanceTransaction
        {
            UserId = userId,
            Amount = -amount,
            Type = TransactionType.Payment,
            Description = description,
            TransactionReference = Guid.NewGuid().ToString()
        };

        user.Balance -= amount;

        _context.BalanceTransactions.Add(transaction);
        await _context.SaveChangesAsync();

        return new BalanceResponseDTO
        {
            Balance = user.Balance,
            UserId = user.Id,
            Username = user.Username
        };
    }

    public async Task<TransactionHistoryResponseDTO> GetTransactionHistoryAsync(string userId, int page = 1, int pageSize = 10)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
        {
            throw new ArgumentException("Пользователь не найден");
        }
        var totalCount = await _context.BalanceTransactions
            .Where(t => t.UserId == userId)
            .CountAsync();

        var transactions = await _context.BalanceTransactions
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new TransactionResponseDTO
            {
                Id = t.Id,
                Amount = t.Amount,
                Type = t.Type,
                Description = t.Description,
                CreatedAt = t.CreatedAt,
                PaymentMethod = t.PaymentMethod,
                TransactionReference = t.TransactionReference
            })
            .ToListAsync();

        return new TransactionHistoryResponseDTO
        {
            Transactions = transactions,
            CurrentBalance = user.Balance,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<bool> HasSufficientBalanceAsync(string userId, decimal amount)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        return user != null && user.Balance >= amount;
    }

    public async Task<BalanceResponseDTO> ProcessPaymentAsync(string userId, decimal amount, TransactionType type, string description, string? paymentMethod = null, string? transactionReference = null)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
        {
            throw new ArgumentException("Пользователь не найден");
        }
        
        var transaction = new BalanceTransaction
        {
            UserId = userId,
            Amount = type == TransactionType.TopUp || type == TransactionType.Refund || type == TransactionType.Bonus ? amount : -amount,
            Type = type,
            Description = description,
            PaymentMethod = paymentMethod,
            TransactionReference = transactionReference ?? Guid.NewGuid().ToString()
        };

        switch (type)
        {
            case TransactionType.TopUp:
            case TransactionType.Refund:
            case TransactionType.Bonus:
                user.Balance += amount;
                break;
            case TransactionType.Payment:
            case TransactionType.Withdrawal:
                if (user.Balance < amount)
                {
                    throw new InvalidOperationException("Недостаточно средств на балансе");
                } 
                user.Balance -= amount;
                break;
        }

        _context.BalanceTransactions.Add(transaction);
        await _context.SaveChangesAsync();

        return new BalanceResponseDTO
        {
            Balance = user.Balance,
            UserId = user.Id,
            Username = user.Username
        };
    }

    public (bool IsValid, string ErrorMessage) ValidateTopUpAmount(decimal amount)
    {
        if (amount <= 0){ return (false, "Сумма пополнения должна быть больше нуля"); }
            
        if (amount < 30){ return (false, "Минимальная сумма пополнения: 30 долларов"); }

        if (amount > 100000){ return (false, "Максимальная сумма пополнения: 100,000 долларов"); }

        return (true, string.Empty);
    }

    public async Task<(bool IsValid, string ErrorMessage)> ValidateDeductAmountAsync(string userId, decimal amount)
    {
        if (amount <= 0){ return (false, "Сумма списания должна быть больше нуля"); }
        
        var user = await _context.Users.FindAsync(userId);
        if (user == null){ return (false, "Пользователь не найден");  }

        if (user.Balance < amount){ return (false, "Недостаточно средств на балансе"); }

        return (true, string.Empty);
    }
}
