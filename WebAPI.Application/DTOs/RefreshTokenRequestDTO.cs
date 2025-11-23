using System.ComponentModel.DataAnnotations;

namespace WebAPI.Application.DTOs;

public class RefreshTokenRequestDTO
{
    [Required(ErrorMessage = "Refresh token is required")]
    public string RefreshToken { get; set; }
}
