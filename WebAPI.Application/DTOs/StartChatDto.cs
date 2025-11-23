namespace WebAPI.Application.DTOs;

public record StartChatDto(string FromUserId, string ToUserId, string? InitialMessage);