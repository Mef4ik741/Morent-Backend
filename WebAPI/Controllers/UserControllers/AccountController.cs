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
    private readonly IEmailService _emailService;
    
    public AccountController(IAccountService accountService, ITokenService tokenService, IEmailService emailService)
    {
        _accountService = accountService;
        _tokenService = tokenService;
        _emailService = emailService;
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

    [HttpPost("forgot-password-by-username")]
    public async Task<IActionResult> ForgotPasswordByUsernameAsync([FromBody] ForgotPasswordByUsernameDTO request)
    {
        var result = await _accountService.ForgotPasswordByUsernameAsync(request);
        
        if (!result.IsSuccess){ return BadRequest(new { result.Message }); }
        return Ok(new { result.Message });
    }

    [HttpPost("verify-reset-token-by-username")]
    public async Task<IActionResult> VerifyResetTokenByUsernameAsync([FromBody] VerifyResetTokenByUsernameDTO request)
    {
        var result = await _accountService.VerifyResetTokenByUsernameAsync(request);
        
        if (!result.IsSuccess)
            return BadRequest(new { result.Message });
            
        return Ok(new { result.Message, Valid = true });
    }

    [HttpPost("reset-password-by-username")]
    public async Task<IActionResult> ResetPasswordByUsernameAsync([FromBody] ResetPasswordByUsernameDTO request)
    {
        var result = await _accountService.ResetPasswordByUsernameAsync(request);
        
        if (!result.IsSuccess){ return BadRequest(new { result.Message }); }
            
        return Ok(new { result.Message });
    }

    [HttpPost("send-linking-code")]
    public async Task<IActionResult> SendLinkingCode([FromBody] SendLinkingCodeDTO request)
    {
        var result = await _accountService.SendLinkingCodeAsync(request);
        
        if (!result.IsSuccess) { return BadRequest(new { result.Message }); }
        return Ok(new { result.Message });
    }

    [HttpPost("verify-linking-code")]
    public async Task<IActionResult> VerifyLinkingCode([FromBody] VerifyLinkingCodeDTO request)
    {
        var result = await _accountService.VerifyLinkingCodeAsync(request);
        
        if (!result.IsSuccess){ return BadRequest(new { result.Message }); }
        
        return Ok(new { result.Message, Success = true });
    }

    [Authorize]
    [HttpGet("profile")]
    public async Task<IActionResult> GetUserProfile()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userId)){ return Unauthorized(new { Message = "Не удалось идентифицировать пользователя" }); }
        var profile = await _accountService.GetProfileAsync(userId);
        
        if (profile == null) { return NotFound(new { Message = "Пользователь не найден" }); }
        return Ok(profile);
    }
}