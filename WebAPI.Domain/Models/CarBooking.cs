namespace WebAPI.Domain.Models;

public class CarBooking
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    public string CarId { get; set; }
    public Car Car { get; set; }

    public string RenterUserId { get; set; }

    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal TotalPrice { get; set; }

    public bool Agreement { get; set; } = false;
    public bool StatusActive { get; set; } = true;

    public List<string> Locations { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
