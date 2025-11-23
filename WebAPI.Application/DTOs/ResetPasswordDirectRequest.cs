using System.ComponentModel.DataAnnotations;

namespace WebAPI.Application.DTOs;

public class ResetPasswordDirectRequest
{
    [Required(ErrorMessage = "Username обязателен")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Новый пароль обязателен")]
    [MinLength(6, ErrorMessage = "Пароль должен содержать минимум 6 символов")]
    public string NewPassword { get; set; } = string.Empty;
}
