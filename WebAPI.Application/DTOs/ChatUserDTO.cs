namespace WebAPI.Application.DTOs;

public record ChatUserDTO(
    string UserId,
    string Username,
    string Name,
    string Surname,
    bool IsOnline
);


