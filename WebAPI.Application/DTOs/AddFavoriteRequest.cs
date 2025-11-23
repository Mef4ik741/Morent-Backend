namespace WebAPI.Application.DTOs;

public class AddFavoriteRequest
{
    public string UserId { get; set; } = null!;
    public string CarId { get; set; } = null!;
}
