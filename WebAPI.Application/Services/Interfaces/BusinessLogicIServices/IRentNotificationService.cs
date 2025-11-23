using WebAPI.Application.DTOs;

namespace WebAPI.Application.Services.Interfaces.BusinessLogicIServices;

public interface IRentNotificationService
{
    Task<IEnumerable<RentNotificationDto>> GetMyNotificationsAsync(string userId);
    Task<int> GetUnreadCountAsync(string userId);
    Task<bool> MarkAsReadAsync(string notificationId, string userId);
    Task<bool> MarkAllAsReadAsync(string userId);
    Task<RentNotificationDto?> RespondToRentRequestAsync(string bookingId, string userId, bool isApproved, string? message);
    Task<bool> DeleteNotificationAsync(string notificationId, string userId);
    Task SendRentRequestNotificationAsync(string bookingId, string ownerUserId, string renterUserId);
}
