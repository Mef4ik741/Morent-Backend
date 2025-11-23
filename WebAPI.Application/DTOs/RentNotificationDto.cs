namespace WebAPI.Application.DTOs;

/// <summary>
/// DTO для уведомлений о запросах на аренду
/// </summary>
public class RentNotificationDto
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string BookingId { get; set; } = null!;
    public string CarId { get; set; } = null!;
    public string CarName { get; set; } = null!;
    public string CarBrand { get; set; } = null!;
    public string? CarImageUrl { get; set; }
    public string RenterUserId { get; set; } = null!;
    public string RenterName { get; set; } = null!;
    public string? RenterEmail { get; set; }
    public string OwnerUserId { get; set; } = null!;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal TotalPrice { get; set; }
    public string NotificationType { get; set; } = "RentRequest";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsRead { get; set; } = false;
    public string Message { get; set; } = null!;
}

/// <summary>
/// DTO для ответа владельца на запрос аренды
/// </summary>
public class RentResponseDto
{
    public string BookingId { get; set; } = null!;
    public bool IsApproved { get; set; }
    public string? Message { get; set; }
}
