namespace WebAPI.Application.DTOs;

public record AdminInfoDto(
    string Id,
    string Name,
    string Surname,
    string Username,
    string Email,
    List<string> Roles
);