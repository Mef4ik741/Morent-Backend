namespace WebAPI.Application.DTOs;

public record UpdateRentDTO(
    string UserId,
    string CarId,
    DateTime StartDate,
    DateTime EndDate,
    bool Agreement,
    bool StatusRent,
    List<string>? Locations = null
);
