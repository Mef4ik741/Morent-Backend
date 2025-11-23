using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;
using WebAPI.Application.DTOs;
using WebAPI.Domain.Models;
using WebAPI.Infrastructure.Data.Context;

namespace WebAPI.Application.Hubs;

public class RentNotificationHub : Hub
{
    private static readonly ConcurrentDictionary<string, string> UserConnections = new();
    private readonly Context _context;

    public RentNotificationHub(Context context)
    {
        _context = context;
    }

    /// <summary>
    /// Пользователь присоединяется к получению уведомлений
    /// </summary>
    public async Task JoinNotifications(string userId)
    {
        UserConnections[userId] = Context.ConnectionId;
        await Groups.AddToGroupAsync(Context.ConnectionId, $"User_{userId}");
        
        var unreadCount = await _context.RentNotifications
            .CountAsync(n => n.OwnerUserId == userId && !n.IsRead);
        
        await Clients.Caller.SendAsync("UnreadNotificationsCount", unreadCount);
    }

    /// <summary>
    /// Отправка уведомления владельцу о новом запросе на аренду
    /// </summary>
    public async Task SendRentRequest(string bookingId, string ownerUserId, string renterUserId)
    {
        try
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

            await Clients.Group($"User_{ownerUserId}").SendAsync("ReceiveRentRequest", notificationDto);
            
            var unreadCount = await _context.RentNotifications
                .CountAsync(n => n.OwnerUserId == ownerUserId && !n.IsRead);
            await Clients.Group($"User_{ownerUserId}").SendAsync("UnreadNotificationsCount", unreadCount);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[RentNotificationHub] Error in SendRentRequest: {ex.Message}");
        }
    }

    /// <summary>
    /// Владелец отвечает на запрос аренды (одобрение/отклонение)
    /// </summary>
    public async Task RespondToRentRequest(string notificationId, bool isApproved, string message = "")
    {
        try
        {
            var notification = await _context.RentNotifications
                .Include(n => n.Booking)
                .ThenInclude(b => b.Car)
                .FirstOrDefaultAsync(n => n.Id == notificationId);

            if (notification == null) return;

            var booking = notification.Booking;
            if (booking == null) return;

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
                    ? $"Ваш запрос на аренду {booking.Car.Name} одобрен!" 
                    : $"Ваш запрос на аренду {booking.Car.Name} отклонен.",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            if (!string.IsNullOrEmpty(message))
            {
                responseNotification.Message += $" Сообщение от владельца: {message}";
            }

            _context.RentNotifications.Add(responseNotification);
            
            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var owner = await _context.Users.FindAsync(notification.OwnerUserId);

            var responseDto = new RentNotificationDto
            {
                Id = responseNotification.Id,
                BookingId = notification.BookingId,
                CarId = notification.CarId,
                CarName = booking.Car.Name,
                CarBrand = booking.Car.Brand,
                CarImageUrl = booking.Car.ImageUrl,
                RenterUserId = notification.RenterUserId,
                RenterName = owner?.Name ?? owner?.Username ?? "Владелец",
                OwnerUserId = notification.OwnerUserId,
                StartDate = booking.StartDate,
                EndDate = booking.EndDate,
                TotalPrice = booking.TotalPrice,
                NotificationType = responseNotification.NotificationType,
                CreatedAt = responseNotification.CreatedAt,
                IsRead = false,
                Message = responseNotification.Message
            };

            await Clients.Group($"User_{notification.RenterUserId}").SendAsync("ReceiveRentResponse", responseDto);
            
            var unreadCount = await _context.RentNotifications
                .CountAsync(n => n.OwnerUserId == notification.OwnerUserId && !n.IsRead);
            await Clients.Group($"User_{notification.OwnerUserId}").SendAsync("UnreadNotificationsCount", unreadCount);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[RentNotificationHub] Error in RespondToRentRequest: {ex.Message}");
        }
    }

    /// <summary>
    /// Пометить уведомление как прочитанное
    /// </summary>
    public async Task MarkAsRead(string notificationId)
    {
        try
        {
            var notification = await _context.RentNotifications.FindAsync(notificationId);
            if (notification != null && !notification.IsRead)
            {
                notification.IsRead = true;
                notification.ReadAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                // Обновляем счетчик
                var unreadCount = await _context.RentNotifications
                    .CountAsync(n => n.OwnerUserId == notification.OwnerUserId && !n.IsRead);
                await Clients.Group($"User_{notification.OwnerUserId}").SendAsync("UnreadNotificationsCount", unreadCount);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[RentNotificationHub] Error in MarkAsRead: {ex.Message}");
        }
    }

    /// <summary>
    /// Получить все уведомления пользователя
    /// </summary>
    public async Task GetMyNotifications(string userId)
    {
        try
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

            await Clients.Caller.SendAsync("MyNotifications", notificationDtos);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[RentNotificationHub] Error in GetMyNotifications: {ex.Message}");
        }
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = UserConnections.FirstOrDefault(x => x.Value == Context.ConnectionId).Key;
        if (!string.IsNullOrEmpty(userId))
        {
            UserConnections.TryRemove(userId, out _);
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Статический метод для отправки уведомления из контроллера
    /// </summary>
    public static async Task NotifyOwnerAboutRentRequest(
        IHubContext<RentNotificationHub> hubContext,
        Context context,
        string bookingId,
        string ownerUserId,
        string renterUserId)
    {
        try
        {
            var booking = await context.CarBookings
                .Include(b => b.Car)
                .FirstOrDefaultAsync(b => b.Id == bookingId);

            if (booking == null) return;

            var renter = await context.Users.FindAsync(renterUserId);
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

            context.RentNotifications.Add(notification);
            await context.SaveChangesAsync();

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

            await hubContext.Clients.Group($"User_{ownerUserId}").SendAsync("ReceiveRentRequest", notificationDto);
            
            var unreadCount = await context.RentNotifications
                .CountAsync(n => n.OwnerUserId == ownerUserId && !n.IsRead);
            await hubContext.Clients.Group($"User_{ownerUserId}").SendAsync("UnreadNotificationsCount", unreadCount);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[RentNotificationHub] Error in NotifyOwnerAboutRentRequest: {ex.Message}");
        }
    }
}
