namespace WebAPI.Domain.Models;

public class Favorites
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string UserId { get; set; } = null!;
    
    public string CarId { get; set; } = null!;
    
    public virtual Car Car { get; set; } = null!;
}