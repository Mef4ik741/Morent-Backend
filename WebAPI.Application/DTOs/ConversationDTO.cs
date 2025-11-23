namespace WebAPI.Application.DTOs;

public record ConversationDTO(
    string ConversationId,
    string OtherUserId,
    string OtherUserName,
    string LastMessage,
    DateTime LastMessageTime,
    int UnreadCount
);