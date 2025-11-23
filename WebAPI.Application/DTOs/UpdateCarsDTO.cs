namespace WebAPI.Application.DTOs;

public record UpdateCarsDTO(
    string Name,
    string Brand,
    string Model,
    int Year,
    decimal Price,
    string Description,
    string? Location = null,
    string? ImageUrl = null, // legacy primary image
    string? PrimaryImageUrl = null,
    IEnumerable<string>? GalleryImageUrls = null
);