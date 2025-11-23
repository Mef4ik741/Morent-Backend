namespace WebAPI.Application.DTOs;

public record CreateRentDTO(
    string UserId,
    string CarId,
    DateTime StartDate,
    DateTime EndDate,
    bool Agreement = false,
    List<string>? Locations = null
);
