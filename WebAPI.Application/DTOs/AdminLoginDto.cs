namespace WebAPI.Application.DTOs;

public record AdminLoginDto(
    string UsernameOrEmail,
    string Password
);