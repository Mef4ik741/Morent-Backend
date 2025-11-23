namespace WebAPI.Domain.Models;

/// <summary>
/// Модель для хранения уведомлений о запросах на аренду
/// </summary>
public class RentNotification
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    public string BookingId { get; set; } = null!;
    public virtual CarBooking? Booking { get; set; }
    
    public string CarId { get; set; } = null!;
    public virtual Car? Car { get; set; }
    
    public string RenterUserId { get; set; } = null!;
    public string OwnerUserId { get; set; } = null!;
    
    public string NotificationType { get; set; } = "RentRequest"; // RentRequest, RentApproved, RentRejected, RentCancelled
    public string Message { get; set; } = null!;
    
    public bool IsRead { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReadAt { get; set; }
}
