namespace WebAPI.Application.DTOs;

public record CarResponseDTO(
    string Id,
    string Name,
    string Brand,
    string Model,
    int Year,
    decimal Price,
    string Description,
    string Location,
    bool IsAvailable,
    string PrimaryImageUrl,
    string OwnerUserId,
    List<CarImageDTO> ImageUrls,
    string Category   // ← добавили поле Category
);
