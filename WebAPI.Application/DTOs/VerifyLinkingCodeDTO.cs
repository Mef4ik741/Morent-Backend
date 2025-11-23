namespace WebAPI.Application.DTOs;

public class VerifyLinkingCodeDTO
{
    public string Username { get; set; } = string.Empty;
    public string LinkingCode { get; set; } = string.Empty;
}
