namespace WebAPI.Application.DTOs;

public record BookingResponseDTO(
    string Id,
    string RenterUserId,
    string CarId,
    string? CarName,
    string? CarBrand,
    DateTime StartDate,
    DateTime EndDate,
    decimal TotalPrice,
    bool Agreement,
    bool StatusActive,
    List<string> Locations
);
