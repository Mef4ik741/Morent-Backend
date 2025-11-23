namespace WebAPI.Application.DTOs;

public record AdminLoginResponseDto(
    string AccessToken,
    AdminInfoDto Admin
);