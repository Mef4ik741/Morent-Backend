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

    [HttpGet("debug-token")]
    [AllowAnonymous]
    public IActionResult DebugToken([FromHeader(Name = "Authorization")] string? authHeader)
    {
        try
        {
            var claims = User.Claims.Select(c => new { c.Type, c.Value }).ToList();
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("id")?.Value;
            var userName = User.FindFirstValue(ClaimTypes.Name);
            var email = User.FindFirstValue(ClaimTypes.Email);
            
            // Decode JWT token manually for debugging
            string? tokenPayload = null;
            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
            {
                var token = authHeader.Substring(7);
                try
                {
                    var parts = token.Split('.');
                    if (parts.Length >= 2)
                    {
                        var payload = parts[1];
                        // Add padding if needed
                        while (payload.Length % 4 != 0)
                            payload += "=";
                        
                        var bytes = Convert.FromBase64String(payload);
                        tokenPayload = System.Text.Encoding.UTF8.GetString(bytes);
                    }
                }
                catch (Exception ex)
                {
                    tokenPayload = $"Error decoding: {ex.Message}";
                }
            }
            
            return Ok(new 
            {
                User.Identity?.IsAuthenticated,
                UserId = userId,
                UserName = userName,
                Email = email,
                Claims = claims,
                TokenPayload = tokenPayload,
                AuthHeader = authHeader?.Substring(0, Math.Min(50, authHeader.Length)) + "..."
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Ошибка при отладке токена: {ex.Message}");
        }
    }

    [HttpGet("test-jwt")]
    [AllowAnonymous]
    public IActionResult TestJWT()
    {
        try
        {
            var secretKey = _configuration["JWT:SecretKey"];
            var issuer = _configuration["JWT:Issuer"];
            var audience = _configuration["JWT:Audience"];

            // Create a test token
            var claims = new List<Claim>
            {
                new(ClaimTypes.Name, "TestUser"),
                new(ClaimTypes.NameIdentifier, "test-user-id"),
                new(ClaimTypes.Email, "test@example.com")
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey ?? ""));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(30),
                signingCredentials: creds
            );

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

            // Try to validate the same token
            var tokenHandler = new JwtSecurityTokenHandler();
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = issuer,
                ValidAudience = audience,
                IssuerSigningKey = key,
                ClockSkew = TimeSpan.Zero
            };

            try
            {
                var principal = tokenHandler.ValidateToken(tokenString, validationParameters, out SecurityToken validatedToken);
                
                return Ok(new
                {
                    Success = true,
                    Token = tokenString,
                    SecretKey = secretKey?.Substring(0, 10) + "...",
                    Issuer = issuer,
                    Audience = audience,
                    ValidationResult = "Token validated successfully",
                    Claims = principal.Claims.Select(c => new { c.Type, c.Value }).ToList()
                });
            }
            catch (Exception validationEx)
            {
                return Ok(new
                {
                    Success = false,
                    Token = tokenString,
                    SecretKey = secretKey?.Substring(0, 10) + "...",
                    Issuer = issuer,
                    Audience = audience,
                    ValidationError = validationEx.Message
                });
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Ошибка при тестировании JWT: {ex.Message}");
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
