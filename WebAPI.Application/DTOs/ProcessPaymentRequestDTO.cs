using WebAPI.Domain.enums;

namespace WebAPI.Application.DTOs;

public record ProcessPaymentRequestDTO(decimal Amount, TransactionType Type, string Description,
    string? PaymentMethod, string? TransactionReference);