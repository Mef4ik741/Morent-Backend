using System.ComponentModel.DataAnnotations;

namespace WebAPI.Application.DTOs;

public class UpdateUsernameRequestDTO
{
    [Required(ErrorMessage = "UserId is required")]
    public string UserId { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Username is required")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Username must be between 3 and 50 characters")]
    public string Username { get; set; } = string.Empty;
}
