namespace WebAPI.Application.DTOs;

public record FavoriteCarDTO(
    string CarId,
    string Name,
    decimal Price,
    string? Location,
    string? PrimaryImageUrl
);
