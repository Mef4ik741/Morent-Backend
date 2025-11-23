namespace WebAPI.Application.DTOs;

public class SendLinkingCodeDTO
{
    public string? Username { get; set; }
    public long TelegramId { get; set; }
    public string Code { get; set; }
}
