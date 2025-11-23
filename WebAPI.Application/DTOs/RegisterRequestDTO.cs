namespace WebAPI.Application.DTOs;

public record RegisterRequestDTO(string Name, string Surname, string Email, string Username,
    string Password, string ConfirmPassword);
    
    