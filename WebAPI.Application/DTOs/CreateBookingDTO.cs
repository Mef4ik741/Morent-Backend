namespace WebAPI.Application.DTOs;

public record CreateBookingDTO(
    string RenterUserId,
    string CarId,
    DateTime StartDate,
    DateTime EndDate,
    bool Agreement = false,
    List<string>? Locations = null
);
