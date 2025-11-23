namespace WebAPI.Application.DTOs;

public record RentResponseDTO(
    string Id,
    string UserId,
    string CarId,
    string? CarName,
    string? CarBrand,
    DateTime StartDate,
    DateTime EndDate,
    decimal TotalPrice,
    bool Agreement,
    bool StatusRent,
    List<string> Locations
);
