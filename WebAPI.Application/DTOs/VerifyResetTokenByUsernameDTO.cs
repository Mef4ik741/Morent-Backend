namespace WebAPI.Application.DTOs;

public class VerifyResetTokenByUsernameDTO
{
    public string Username { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
}
