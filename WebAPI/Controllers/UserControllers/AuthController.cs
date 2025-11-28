using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WebAPI.Application.DTOs;
using WebAPI.Application.Services.Interfaces.AccountDirectoryIServices;

namespace WebAPI.Controllers.UserControllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("Login")]
    public async Task<IActionResult> LoginAsync([FromBody] LoginRequestDTO request)
    {
        var res = await _authService.LoginAsync(request);

        if (res.IsSuccess)
        {
            return Ok(res);
        }
        
        return BadRequest(res);
    }

    [HttpPost("LoginByUsername")]
    public async Task<IActionResult> LoginByUsernameAsync([FromBody] LoginByUsernameRequestDTO request)
    {
        var res = await _authService.LoginByUsernameAsync(request);

        if (res.IsSuccess)
        {
            return Ok(res);
        }
        
        return BadRequest(res);
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshTokenAsync([FromBody] RefreshTokenRequestDTO request)
    {
        try
        {
            var result = await _authService.RefreshTokenAsync(request.RefreshToken);
            
            if (result.IsSuccess)
            {
                return Ok(result);
            }
            
            return BadRequest(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Внутренняя ошибка сервера: {ex.Message}");
        }
    }

    [HttpPost("revoke")]
    public async Task<IActionResult> RevokeTokenAsync([FromBody] RevokeTokenRequestDTO request)
    {
        try
        {
            var result = await _authService.RevokeTokenAsync(request.RefreshToken);
            
            if (result.IsSuccess)
            {
                return Ok(result);
            }
            
            return BadRequest(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Внутренняя ошибка сервера: {ex.Message}");
        }
    }

    [HttpPost("revoke-all")]
    [Authorize]
    public async Task<IActionResult> RevokeAllUserTokensAsync()
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            if (string.IsNullOrEmpty(userId)) { return Unauthorized("Пользователь не авторизован"); }
            
            var result = await _authService.RevokeAllUserTokensAsync(userId);
            
            if (result.IsSuccess) { return Ok(result); }
            
            return BadRequest(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Внутренняя ошибка сервера: {ex.Message}");
        }
    }
}