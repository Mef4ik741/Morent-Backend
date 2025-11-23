namespace WebAPI.Application.DTOs;

public record TopBookedCarDTO(
    string CarId,
    string? ImageUrl,
    string? Name,
    string? Brand,
    string? Model,
    string? Location,
    int RentCount
);
