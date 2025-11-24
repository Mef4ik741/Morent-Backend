using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using WebAPI.Application.DTOs;
using WebAPI.Application.Services.Interfaces.BusinessLogicIServices;

namespace WebAPI.Controllers.CarControllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FavoritesController : ControllerBase
{
    private readonly IFavoritesService _favoritesService;
    private readonly IConfiguration _configuration;

    public FavoritesController(IFavoritesService favoritesService, IConfiguration configuration)
    {
        _favoritesService = favoritesService;
        _configuration = configuration;
    }

    [HttpPost("add")]
    public async Task<IActionResult> Add([FromBody] AddFavoriteRequest request)
    {
        try
        {
            var userIdFromToken = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("id")?.Value;
            var userId = !string.IsNullOrWhiteSpace(userIdFromToken) ? userIdFromToken : request.UserId;

            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(request.CarId))
            {
                return BadRequest("UserId и CarId обязательны");
            }

            var ok = await _favoritesService.AddToFavoritesAsync(userId, request.CarId);
            if (!ok) return BadRequest("Не удалось добавить в избранное (проверьте, что автомобиль существует)");
            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Ошибка при добавлении в избранное: {ex.Message}");
        }
    }

    [HttpDelete("remove")]
    public async Task<IActionResult> Remove([FromQuery] string carId, [FromQuery] string? userId)
    {
        try
        {
            var userIdFromToken = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("id")?.Value;
            var effectiveUserId = !string.IsNullOrWhiteSpace(userIdFromToken) ? userIdFromToken : userId;

            if (string.IsNullOrWhiteSpace(effectiveUserId) || string.IsNullOrWhiteSpace(carId))
            {
                return BadRequest("UserId и CarId обязательны");
            }

            var ok = await _favoritesService.RemoveFromFavoritesAsync(effectiveUserId, carId);
            if (!ok) return NotFound("Запись избранного не найдена");
            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Ошибка при удалении из избранного: {ex.Message}");
        }
    }

    [HttpGet("list")]
    public async Task<IActionResult> ListMine()
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("id")?.Value;
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized("Не удалось определить пользователя из токена");

            var items = await _favoritesService.GetFavoritesAsync(userId);
            return Ok(items);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Ошибка при получении избранного: {ex.Message}");
        }
    }

    [HttpGet("{userId}")]
    [Authorize(Roles = "AppAdmin,AppSuperAdmin")] 
    public async Task<IActionResult> ListByUser(string userId)
    {
        try
        {
            var items = await _favoritesService.GetFavoritesAsync(userId);
            return Ok(items);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Ошибка при получении избранного пользователя: {ex.Message}");
        }
    }
}
