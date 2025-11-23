using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WebAPI.Application.DTOs;
using WebAPI.Application.Services.Interfaces.BusinessLogicIServices;

namespace WebAPI.Controllers.SignalRControllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RentNotificationController : ControllerBase
{
    private readonly IRentNotificationService _notificationService;

    public RentNotificationController(IRentNotificationService notificationService)
    {
        _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
    }

    private string? GetUserId() => User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    /// <summary>
    /// Получить все уведомления текущего пользователя
    /// </summary>
    [HttpGet("my-notifications")]
    public async Task<IActionResult> GetMyNotifications()
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { message = "Пользователь не авторизован" });
        }
        
        var notifications = await _notificationService.GetMyNotificationsAsync(userId);
        return Ok(new { success = true, data = notifications });
    }

    /// <summary>
    /// Получить количество непрочитанных уведомлений
    /// </summary>
    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount()
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { message = "Пользователь не авторизован" });
        }
        
        var count = await _notificationService.GetUnreadCountAsync(userId);
        return Ok(new { success = true, count });
    }

    /// <summary>
    /// Пометить уведомление как прочитанное
    /// </summary>
    [HttpPut("mark-as-read/{notificationId}")]
    public async Task<IActionResult> MarkAsRead(string notificationId)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
            return Unauthorized(new { message = "Пользователь не авторизован" });
        
        var result = await _notificationService.MarkAsReadAsync(notificationId, userId);
        if (!result)
            return NotFound(new { success = false, message = "Уведомление не найдено или нет доступа" });

        return Ok(new { success = true, message = "Уведомление помечено как прочитанное" });
    }

    /// <summary>
    /// Пометить все уведомления как прочитанные
    /// </summary>
    [HttpPut("mark-all-as-read")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId)){ return Unauthorized(new { message = "Пользователь не авторизован" }); }

        await _notificationService.MarkAllAsReadAsync(userId);
        return Ok(new { success = true, message = "Все уведомления помечены как прочитанные" });
    }

    /// <summary>
    /// Ответить на запрос аренды (одобрить/отклонить)
    /// </summary>
    [HttpPost("respond")]
    public async Task<IActionResult> RespondToRentRequest([FromBody] RentResponseDto response)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
            return Unauthorized(new { message = "Пользователь не авторизован" });

        var result = await _notificationService.RespondToRentRequestAsync(
            response.BookingId, 
            userId, 
            response.IsApproved, 
            response.Message);

        if (result == null){ return NotFound(new { success = false, message = "Уведомление не найдено или у вас нет прав" }); }

        return Ok(new 
        { 
            success = true, 
            message = response.IsApproved ? "Запрос одобрен" : "Запрос отклонен",
            data = result
        });
    }

    /// <summary>
    /// Удалить уведомление
    /// </summary>
    [HttpDelete("{notificationId}")]
    public async Task<IActionResult> DeleteNotification(string notificationId)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId)){ return Unauthorized(new { message = "Пользователь не авторизован" }); }
        
        var result = await _notificationService.DeleteNotificationAsync(notificationId, userId);
        if (!result){ return NotFound(new { success = false, message = "Уведомление не найдено или нет доступа" }); }
        return Ok(new { success = true, message = "Уведомление удалено" });
    }
}
