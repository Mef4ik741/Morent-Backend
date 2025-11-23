namespace WebAPI.Application.DTOs;

public record AdminRegisterDto(
    string Name,
    string Surname,
    string Username,
    string Email,
    string Password
);