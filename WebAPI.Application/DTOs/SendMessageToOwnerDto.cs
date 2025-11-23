namespace WebAPI.Application.DTOs;

public class SendMessageToOwnerDto 
{
    public string FromUserId { get; set; } = string.Empty;
    public string OwnerUserId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

