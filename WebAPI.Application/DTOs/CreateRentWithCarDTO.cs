namespace WebAPI.Application.DTOs;

public record CreateRentWithCarDTO(
    string UserId,
    AddedCarsDTO Car,
    DateTime StartDate,
    DateTime EndDate,
    bool Agreement = false,
    List<string>? Locations = null
);
