namespace WebAPI.Application.DTOs;

public record CarResponseDTO(
    string Id,
    string Name,
    string Brand,
    string Model,
    int Year,
    decimal Price,
    string Description,
    string? Location = null,
    bool IsAvailable = true,
    string? ImageUrl = null,
    string OwnerUserId = null!,
    IEnumerable<CarImageDTO>? Images = null
);