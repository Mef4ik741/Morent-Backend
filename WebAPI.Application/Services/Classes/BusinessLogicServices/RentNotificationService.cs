using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using WebAPI.Application.DTOs;
using WebAPI.Application.Hubs;
using WebAPI.Application.Services.Interfaces.BusinessLogicIServices;
using WebAPI.Domain.Models;
using WebAPI.Infrastructure.Data.Context;

namespace WebAPI.Application.Services.Classes.BusinessLogicServices;

public class RentNotificationService : IRentNotificationService
{
    private readonly Context _context;
    private readonly IHubContext<RentNotificationHub> _hubContext;
    public RentNotificationService(
        Context context,
        IHubContext<RentNotificationHub> hubContext)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
    }

    public async Task<IEnumerable<RentNotificationDto>> GetMyNotificationsAsync(string userId)
    {
        var notifications = await _context.RentNotifications
            .Where(n => n.OwnerUserId == userId || n.RenterUserId == userId)
            .Include(n => n.Car)
            .Include(n => n.Booking)
            .OrderByDescending(n => n.CreatedAt)
            .Take(50)
            .ToListAsync();

        var notificationDtos = new List<RentNotificationDto>();

        foreach (var notification in notifications)
        {
            var renter = await _context.Users.FindAsync(notification.RenterUserId);
            
            notificationDtos.Add(new RentNotificationDto
            {
                Id = notification.Id,
                BookingId = notification.BookingId,
                CarId = notification.CarId,
                CarName = notification.Car?.Name ?? "Неизвестный автомобиль",
                CarBrand = notification.Car?.Brand ?? "",
                CarImageUrl = notification.Car?.ImageUrl,
                RenterUserId = notification.RenterUserId,
                RenterName = renter?.Name ?? renter?.Username ?? "Пользователь",
                RenterEmail = renter?.Email,
                OwnerUserId = notification.OwnerUserId,
                StartDate = notification.Booking?.StartDate ?? DateTime.UtcNow,
                EndDate = notification.Booking?.EndDate ?? DateTime.UtcNow,
                TotalPrice = notification.Booking?.TotalPrice ?? 0,
                NotificationType = notification.NotificationType,
                CreatedAt = notification.CreatedAt,
                IsRead = notification.IsRead,
                Message = notification.Message
            });
        }

        return notificationDtos;
    }

    public async Task<int> GetUnreadCountAsync(string userId)
    {
        return await _context.RentNotifications
            .CountAsync(n => n.OwnerUserId == userId && !n.IsRead);
    }

    public async Task<bool> MarkAsReadAsync(string notificationId, string userId)
    {
        var notification = await _context.RentNotifications.FindAsync(notificationId);
        if (notification == null)
            return false;

        if (notification.OwnerUserId != userId && notification.RenterUserId != userId)
            return false;

        if (!notification.IsRead)
        {
            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var unreadCount = await GetUnreadCountAsync(userId);
            await _hubContext.Clients.Group($"User_{userId}").SendAsync("UnreadNotificationsCount", unreadCount);
        }

        return true;
    }

    public async Task<bool> MarkAllAsReadAsync(string userId)
    {
        var unreadNotifications = await _context.RentNotifications
            .Where(n => n.OwnerUserId == userId && !n.IsRead)
            .ToListAsync();

        foreach (var notification in unreadNotifications)
        {
            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        await _hubContext.Clients.Group($"User_{userId}").SendAsync("UnreadNotificationsCount", 0);

        return true;
    }

    public async Task<RentNotificationDto?> RespondToRentRequestAsync(string bookingId, string userId, bool isApproved, string? message)
    {
        var notification = await _context.RentNotifications
            .Include(n => n.Booking)
            .ThenInclude(b => b.Car)
            .FirstOrDefaultAsync(n => n.BookingId == bookingId && n.OwnerUserId == userId);

        if (notification == null)
            return null;

        var booking = notification.Booking;
        if (booking == null)
            return null;

        booking.Agreement = isApproved;
        booking.StatusActive = isApproved;

        var responseNotification = new RentNotification
        {
            BookingId = notification.BookingId,
            CarId = notification.CarId,
            RenterUserId = notification.RenterUserId,
            OwnerUserId = notification.OwnerUserId,
            NotificationType = isApproved ? "RentApproved" : "RentRejected",
            Message = isApproved 
                ? $"Ваш запрос на аренду {booking.Car?.Name ?? "автомобиль"} одобрен!" 
                : $"Ваш запрос на аренду {booking.Car?.Name ?? "автомобиль"} отклонен.",
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        if (!string.IsNullOrEmpty(message))
        {
            responseNotification.Message += $" Сообщение от владельца: {message}";
        }

        _context.RentNotifications.Add(responseNotification);
        
        notification.IsRead = true;

        await _context.SaveChangesAsync();

        var owner = await _context.Users.FindAsync(notification.OwnerUserId);

        var responseDto = new RentNotificationDto
        {
            Id = responseNotification.Id,
            BookingId = notification.BookingId,
            CarId = notification.CarId,
            CarName = booking?.Car?.Name ?? "Автомобиль",
            CarBrand = booking?.Car?.Brand ?? "",
            CarImageUrl = booking?.Car?.ImageUrl,
            RenterUserId = notification.RenterUserId,
            RenterName = owner?.Name ?? owner?.Username ?? "Владелец",
            OwnerUserId = notification.OwnerUserId,
            StartDate = booking?.StartDate ?? DateTime.UtcNow,
            EndDate = booking?.EndDate ?? DateTime.UtcNow,
            TotalPrice = booking?.TotalPrice ?? 0,
            NotificationType = responseNotification.NotificationType,
            CreatedAt = responseNotification.CreatedAt,
            IsRead = false,
            Message = responseNotification.Message
        };

        await _hubContext.Clients.Group($"User_{notification.RenterUserId}").SendAsync("ReceiveRentResponse", responseDto);
        
        var unreadCount = await GetUnreadCountAsync(userId);
        await _hubContext.Clients.Group($"User_{userId}").SendAsync("UnreadNotificationsCount", unreadCount);


        return responseDto;
    }

    public async Task<bool> DeleteNotificationAsync(string notificationId, string userId)
    {
        var notification = await _context.RentNotifications.FindAsync(notificationId);
        if (notification == null)
            return false;

        if (notification.OwnerUserId != userId && notification.RenterUserId != userId)
            return false;

        _context.RentNotifications.Remove(notification);
        await _context.SaveChangesAsync();

        var unreadCount = await GetUnreadCountAsync(userId);
        await _hubContext.Clients.Group($"User_{userId}").SendAsync("UnreadNotificationsCount", unreadCount);

        return true;
    }

    public async Task SendRentRequestNotificationAsync(string bookingId, string ownerUserId, string renterUserId)
    {
        var booking = await _context.CarBookings
            .Include(b => b.Car)
            .FirstOrDefaultAsync(b => b.Id == bookingId);

        if (booking == null) return;

        var renter = await _context.Users.FindAsync(renterUserId);
        if (renter == null) return;

        var notification = new RentNotification
        {
            BookingId = bookingId,
            CarId = booking.CarId,
            RenterUserId = renterUserId,
            OwnerUserId = ownerUserId,
            NotificationType = "RentRequest",
            Message = $"{renter.Name ?? renter.Username} хочет арендовать ваш автомобиль {booking.Car.Name}",
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.RentNotifications.Add(notification);
        await _context.SaveChangesAsync();

        var notificationDto = new RentNotificationDto
        {
            Id = notification.Id,
            BookingId = bookingId,
            CarId = booking.CarId,
            CarName = booking.Car.Name,
            CarBrand = booking.Car.Brand,
            CarImageUrl = booking.Car.ImageUrl,
            RenterUserId = renterUserId,
            RenterName = renter.Name ?? renter.Username,
            RenterEmail = renter.Email,
            OwnerUserId = ownerUserId,
            StartDate = booking.StartDate,
            EndDate = booking.EndDate,
            TotalPrice = booking.TotalPrice,
            NotificationType = "RentRequest",
            CreatedAt = notification.CreatedAt,
            IsRead = false,
            Message = notification.Message
        };

        await _hubContext.Clients.Group($"User_{ownerUserId}").SendAsync("ReceiveRentRequest", notificationDto);
        
        var unreadCount = await GetUnreadCountAsync(ownerUserId);
        await _hubContext.Clients.Group($"User_{ownerUserId}").SendAsync("UnreadNotificationsCount", unreadCount);

    }
}
