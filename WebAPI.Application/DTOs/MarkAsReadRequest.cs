namespace WebAPI.Application.DTOs;

public record MarkAsReadRequest(
    string UserId1,
    string UserId2
);