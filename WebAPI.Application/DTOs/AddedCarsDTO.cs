namespace WebAPI.Application.DTOs;

public record AddedCarsDTO(
    string Name,
    string Brand,
    string Model,
    int Year,
    decimal Price,
    string Description,
    string? Location,
    string OwnerUserId,
    string? ImageUrl = null,
    string? PrimaryImageUrl = null,
    IEnumerable<string>? GalleryImageUrls = null
);