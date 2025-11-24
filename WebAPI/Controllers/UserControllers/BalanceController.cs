/*using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WebAPI.Application.DTOs;
using WebAPI.Application.Services.Interfaces.AccountDirectoryIServices;

namespace WebAPI.Controllers.UserControllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BalanceController : ControllerBase
{
    private readonly IBalanceService _balanceService;

    public BalanceController(IBalanceService balanceService)
    {
        _balanceService = balanceService;
    }

    [HttpGet("my-balance")]
    public async Task<ActionResult<BalanceResponseDTO>> GetMyBalance()
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("Пользователь не авторизован");
            }

            var balance = await _balanceService.GetUserBalanceAsync(userId);
            return Ok(balance);
        }
        catch (ArgumentException ex)
        {
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Внутренняя ошибка сервера: {ex.Message}");
        }
    }

    [HttpPost("top-up")]
    public async Task<ActionResult<BalanceResponseDTO>> TopUpBalance([FromBody] TopUpBalanceRequestDTO request)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("Пользователь не авторизован");
            }
            
            var validation = _balanceService.ValidateTopUpAmount(request.Amount);
            if (!validation.IsValid)
            {
                return BadRequest(validation.ErrorMessage);
            }
            
            var result = await _balanceService.TopUpBalanceAsync(userId, request);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Внутренняя ошибка сервера: {ex.Message}");
        }
    }

    [HttpPost("deduct")]
    [Authorize(Roles = "UserVerified")]
    public async Task<ActionResult<BalanceResponseDTO>> DeductBalance([FromBody] decimal amount, [FromQuery] string description = "Списание средств")
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("Пользователь не авторизован");
            }
            var validation = await _balanceService.ValidateDeductAmountAsync(userId, amount);
            if (!validation.IsValid)
            {
                return BadRequest(validation.ErrorMessage);
            }

            var result = await _balanceService.DeductBalanceAsync(userId, amount, description);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Внутренняя ошибка сервера: {ex.Message}");
        }
    }

    [HttpGet("transactions")]
    public async Task<ActionResult<TransactionHistoryResponseDTO>> GetTransactionHistory([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)){ return Unauthorized("Пользователь не авторизован"); }

            if (page < 1)
            { page = 1; }
            if (pageSize < 1 || pageSize > 100) { pageSize = 10; }

            var history = await _balanceService.GetTransactionHistoryAsync(userId, page, pageSize);
            return Ok(history);
        }
        catch (ArgumentException ex)
        {
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Внутренняя ошибка сервера: {ex.Message}");
        }
    }

    [HttpGet("check-balance/{amount}")]
    public async Task<ActionResult<bool>> CheckSufficientBalance(decimal amount)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("Пользователь не авторизован");
            }

            var hasSufficientBalance = await _balanceService.HasSufficientBalanceAsync(userId, amount);
            return Ok(hasSufficientBalance);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Внутренняя ошибка сервера: {ex.Message}");
        }
    }

    [HttpGet("user/{userId}")]
    [Authorize(Roles = "AppAdmin")]
    public async Task<ActionResult<BalanceResponseDTO>> GetUserBalance(string userId)
    {
        try
        {
            var balance = await _balanceService.GetUserBalanceAsync(userId);
            return Ok(balance);
        }
        catch (ArgumentException ex)
        {
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Внутренняя ошибка сервера: {ex.Message}");
        }
    }

    [HttpPost("user/{userId}/process-payment")]
    [Authorize(Roles = "AppAdmin")]
    public async Task<ActionResult<BalanceResponseDTO>> ProcessUserPayment(string userId, [FromBody] ProcessPaymentRequestDTO request)
    {
        try
        {
            var result = await _balanceService.ProcessPaymentAsync(
                userId, 
                request.Amount, 
                request.Type, 
                request.Description, 
                request.PaymentMethod, 
                request.TransactionReference
            );
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Внутренняя ошибка сервера: {ex.Message}");
        }
    }
}*/