using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WebAPI.Application.DTOs;
using WebAPI.Application.Services.Interfaces.BusinessLogicIServices;

namespace WebAPI.Controllers.CarControllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BookingController : ControllerBase
{
    private readonly IBookingsService _bookingsService;

    public BookingController(IBookingsService bookingsService)
    {
        _bookingsService = bookingsService;
    }

    private string? GetUserId() => User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var booking = await _bookingsService.GetByIdAsync(id);
        return booking is null ? NotFound() : Ok(booking);
    }

    [HttpGet("user/{userId}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetByUser(string userId)
    {
        var list = await _bookingsService.GetByUserAsync(userId);
        return Ok(list);
    }

    [HttpGet("car/{carId}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetByCar(string carId)
    {
        var list = await _bookingsService.GetByCarAsync(carId);
        return Ok(list);
    }

    [HttpGet("by-date-range")]
    [Authorize(Roles = "AppAdmin,AppSuperAdmin")]
    public async Task<IActionResult> GetByDateRange([FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
    {
        var list = await _bookingsService.GetByDateRangeAsync(startDate, endDate);
        return Ok(list);
    }

    [HttpGet("owner/requests")]
    [Authorize(Roles = "AppAdmin,AppSuperAdmin,User,UserVerified")]
    public async Task<IActionResult> GetOwnerRequests()
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { message = "Пользователь не авторизован" });
        }

        var list = await _bookingsService.GetOwnerRequestsAsync(userId);
        return Ok(list);
    }

    [HttpGet("owner/{ownerUserId}/brief")]
    [AllowAnonymous]
    public async Task<IActionResult> GetOwnerBrief(string ownerUserId)
    {
        var brief = await _bookingsService.GetOwnerBriefAsync(ownerUserId);
        return brief is null ? NotFound() : Ok(brief);
    }

    [HttpPost("add")]
    [Authorize(Roles = "AppAdmin,AppSuperAdmin,User,UserVerified")]
    public async Task<IActionResult> Create([FromBody] CreateBookingDTO dto)
    {
        try
        {
            if (dto is null) return BadRequest("Данные бронирования не могут быть пустыми");
            var renterUserId = dto.RenterUserId;
            var created = await _bookingsService.CreateAsync(renterUserId, dto);
            return Ok(created);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Ошибка при создании бронирования: {ex.Message}");
        }
    }

    [HttpPost("{id}/cancel")]
    public async Task<IActionResult> Cancel(string id)
    {
        var ok = await _bookingsService.CancelAsync(id);
        return ok ? Ok() : NotFound();
    }

    [HttpPost("{bookingId}/owner-decision")]
    [Authorize(Roles = "AppAdmin,AppSuperAdmin,User,UserVerified")]
    public async Task<IActionResult> OwnerDecision(string bookingId, [FromBody] OwnerDecisionRequest request)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { message = "Пользователь не авторизован" });
        }

        var result = await _bookingsService.RespondToRentRequestAsync(bookingId, userId, request.IsApproved, request.Message);

        if (result == null)
        {
            return NotFound(new { success = false, message = "Бронирование или уведомление не найдено, либо нет прав" });
        }

        return Ok(new
        {
            success = true,
            message = request.IsApproved ? "Запрос одобрен" : "Запрос отклонен",
            data = result
        });
    }

    [HttpGet("{id}/images")]
    [AllowAnonymous]
    public async Task<IActionResult> GetImages(string id)
    {
        try
        {
            var images = await _bookingsService.GetImagesForBookingAsync(id);
            return images is null ? NotFound() : Ok(images);
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

    [HttpGet("top-10")]
    [AllowAnonymous]
    public async Task<IActionResult> GetTop10()
    {
        var list = await _bookingsService.GetTop10BookedCarsAsync();
        return Ok(list);
    }
}
