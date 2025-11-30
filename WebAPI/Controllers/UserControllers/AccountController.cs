using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Application.DTOs;
using WebAPI.Application.Services.Interfaces.AccountDirectoryIServices;

namespace WebAPI.Controllers.UserControllers;

[ApiController]
[Route("api/[controller]")]
public class AccountController : ControllerBase
{
    private readonly IAccountService _accountService;
    private readonly ITokenService _tokenService;

    
    public AccountController(IAccountService accountService, ITokenService tokenService)
    {
        _accountService = accountService;
        _tokenService = tokenService;
    }

    [HttpPost("Register")]
    public async Task<IActionResult> RegisterAsync([FromBody] RegisterRequestDTO request)
    {
        var res = await _accountService.RegisterAsync(request);
        return Ok(res);
    }

    [HttpGet("Email/Verify/{id}/{token}")]
    public async Task<IActionResult> VerifyEmailAsync(string id, string token)
    {
        var res = await _tokenService.ValidateEmailConfirmationTokenAsync(token);
        var emailFromToken = await _tokenService.GetEmailFromToken(token);

        var userId = await _accountService.GetIdByEmailAsync(emailFromToken);

        if (res && userId == id)
        {
            await _accountService.VerifyEmailAsync(userId);
        }
        return Ok(res ? "Email verified successfully" : "Invalid or expired token");
    }

    [Authorize]
    [HttpPost("Email/Confirm")]
    public async Task<IActionResult> ConfirmEmailAsync()
    {
        var token = await _tokenService.CreateEmailConfirmationTokenAsync(User);
        var result = await _accountService.ConfirmEmailAsync(HttpContext, User, token);
        
        if (!result.IsSuccess){ return BadRequest(result.Message); }
            
        return Ok(new { result.Message });
    }

    [HttpPost("UploadAvatar")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadAvatar([FromForm] UploadAvatarDTO request)
    {
        var result = await _accountService.UploadAvatarAsync(request);
        
        if (!result.IsSuccess){ return BadRequest(new { result.Message }); }
            
        return Ok(new { ImageProfileURL = result.Message, Message = "Аватар успешно обновлен" });
    }

    [HttpPut("username")]
    public async Task<IActionResult> UpdateUsername([FromBody] UpdateUsernameRequestDTO request)
    {
        var result = await _accountService.UpdateUsernameAsync(request);
        
        if (!result.IsSuccess){ return BadRequest(new { result.Message }); }
        return Ok(new { result.Message });
    }

    [HttpGet("profile/{userId}")]
    public async Task<IActionResult> GetProfile(string userId)
    {
        var profile = await _accountService.GetProfileAsync(userId);
        
        if (profile == null) { return NotFound(new { Message = "Пользователь не найден" }); }
        return Ok(profile);
    }
    
    [HttpGet("profile")]
    [Authorize]
    public async Task<IActionResult> GetUserProfile()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userId)){ return Unauthorized(new { Message = "Не удалось идентифицировать пользователя" }); }
        var profile = await _accountService.GetProfileAsync(userId);
        
        if (profile == null) { return NotFound(new { Message = "Пользователь не найден" }); }
        return Ok(profile);
    }
}