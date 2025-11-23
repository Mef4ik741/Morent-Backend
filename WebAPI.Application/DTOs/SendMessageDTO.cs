namespace WebAPI.Application.DTOs;

public record SendMessageDto(string FromUserId, string ToUserId, string Message);   