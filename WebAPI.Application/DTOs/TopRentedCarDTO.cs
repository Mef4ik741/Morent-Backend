namespace WebAPI.Application.DTOs;

public record TopRentedCarDTO(
    string CarId,
    string? Name,
    string? Brand,
    string? Location,
    int RentCount
);
