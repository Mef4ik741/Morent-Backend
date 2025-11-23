namespace WebAPI.Application.DTOs;

public record UserSearchResultDto(string Id, string Username, string Name, string Surname, string? 
    ImageProfileURL, bool IsVerified, DateTime LastSeen, bool IsOnline);