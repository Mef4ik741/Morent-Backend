using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WebAPI.Application.DTOs;
using WebAPI.Application.Services.Interfaces.AccountDirectoryIServices;

namespace WebAPI.Controllers.UserControllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;

    public UserController(IUserService userService)
    {
        _userService = userService;
    }

    /// <summary>
    /// Поиск пользователей по никнейму
    /// </summary>
    /// <param name="query">Поисковый запрос (никнейм)</param>
    /// <param name="limit">Максимальное количество результатов (по умолчанию 10)</param>
    /// <returns>Список найденных пользователей</returns>
    [HttpGet("search")]
    public async Task<ActionResult<IEnumerable<UserSearchResultDto>>> SearchUsers(
        [FromQuery] string query, 
        [FromQuery] int limit = 10)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return BadRequest("Query parameter is required");
        }

        if (query.Length < 2)
        {
            return BadRequest("Query must be at least 2 characters long");
        }

        if (limit <= 0 || limit > 50)
        {
            limit = 10;
        }

        try
        {
            var users = await _userService.SearchUsersByUsernameAsync(query, limit);
            return Ok(users);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    /// <summary>
    /// Получить информацию о пользователе по ID
    /// </summary>
    /// <param name="userId">ID пользователя</param>
    /// <returns>Информация о пользователе</returns>
    [HttpGet("{userId}")]
    public async Task<ActionResult<UserSearchResultDto>> GetUser(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return BadRequest("UserId is required");
        }

        try
        {
            var user = await _userService.GetUserByIdAsync(userId);
            if (user == null)
            {
                return NotFound("User not found");
            }

            return Ok(user);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    [HttpPost("verify")]
    [Authorize(Roles = "User")]
    public async Task<IActionResult> GrantUserVerified()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("id")?.Value;

        if (string.IsNullOrWhiteSpace(userId))
        {
            return BadRequest("User id not found in token");
        }
        
        var result = await _userService.GrantUserVerifiedRoleAsync(userId);
        return result;
    }

    [HttpGet("Users")]
    [Authorize(Policy = "AdminPolicy")]
    public async Task<IActionResult> GetUsers(int page = 1, int pageSize = 10)
    {
        return await _userService.GetUsers(page, pageSize);
    }

    [HttpGet("Stats")]
    [Authorize(Policy = "AdminPolicy")]
    public async Task<IActionResult> GetStats()
    {
        return await _userService.GetStats();
    }

    [HttpGet("SearchUsers")]
    [Authorize(Policy = "AdminPolicy")]
    public async Task<IActionResult> SearchUsers(string query, int page = 1, int pageSize = 10)
    {
        return await _userService.SearchUsers(query, page, pageSize);
    }
    
    [HttpGet("GetRoles")]
    [Authorize(Policy = "AdminPolicy")]
    public async Task<IActionResult> GetRoles()
    {
        return await _userService.GetRoles();
    }

    [HttpPost("CreateUser")]
    [Authorize(Policy = "AdminPolicy")]
    public async Task<IActionResult> CreateUser(CreateUserRequest request)
    {
        return await _userService.CreateUser(request);
    }
    
    [HttpPut("UpdateUser")]
    [Authorize(Policy = "AdminPolicy")]
    public async Task<IActionResult> UpdateUser(string id, UpdateUserRequest request)
    {
        return await _userService.UpdateUser(id,request);
    }
    
    [HttpDelete("DeleteUser")]
    [Authorize(Policy = "AdminPolicy")]
    public async Task<IActionResult> DeleteUser(string id)
    {
        return await _userService.DeleteUser(id);
    }

    [HttpGet("GetUsersMonthlyStats")]
    [Authorize(Policy = "AdminPolicy")]
    public async Task<IActionResult> GetUsersMonthlyStats()
    {
        return await _userService.GetUsersMonthlyStats();
    }

    [HttpGet("GetTopUsersByReviews")]
    [Authorize(Policy = "AdminPolicy")]
    public async Task<IActionResult> GetTopUsersByReviews()
    {
        return await _userService.GetTopUsersByReviews();
    }
    
    [HttpGet("GetVerificationStats")]
    [Authorize(Policy = "AdminPolicy")]
    public async Task<IActionResult> GetVerificationStats()
    {
        return await _userService.GetVerificationStats();
    }

    [HttpGet("GetTodayHourlyStats")]
    [Authorize(Policy = "AdminPolicy")]
    public async Task<IActionResult> GetTodayHourlyStats()
    {
        return await _userService.GetTodayHourlyStats();
    }
    
    [HttpGet("GetWeeklyActivity")]
    [Authorize(Policy = "AdminPolicy")]
    public async  Task<IActionResult> GetWeeklyActivity()
    {
        return await _userService.GetWeeklyActivity();
    }
}