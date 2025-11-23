namespace WebAPI.Application.DTOs;

public class ResetPasswordByUsernameDTO
{
    public string Username { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}
