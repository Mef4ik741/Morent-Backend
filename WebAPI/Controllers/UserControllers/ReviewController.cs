using Microsoft.AspNetCore.Mvc;
using WebAPI.Application.DTOs;
using WebAPI.Application.Services.Interfaces.AccountDirectoryIServices;

namespace WebAPI.Controllers.UserControllers;

[ApiController]
[Route("api/[controller]")]
public class ReviewController : ControllerBase
{
    private readonly IReviewService _reviewService;
    public ReviewController(IReviewService reviewService)
    {
        _reviewService = reviewService;
    }

    [HttpPost("rate")]
    public async Task<IActionResult> Rate([FromBody] RateRequestDTO dto)
    {
        if (dto == null) return BadRequest("Пустой запрос");
        var ok = await _reviewService.RateAsync(dto.UserId, dto.ReviewerId, dto.Rating, dto.Comment);
        if (!ok) return BadRequest("Некорректные данные или пользователь не найден");
        return Ok("Оценка сохранена");
    }

    [HttpGet("average/{userId}")]
    public async Task<ActionResult<RatingSummaryDTO>> GetAverage(string userId)
    {
        var (avg, count) = await _reviewService.GetAverageAsync(userId);
        return Ok(new RatingSummaryDTO(userId, avg, count));
    }

    [HttpGet("comments")]
    public async Task<ActionResult<List<UserReviewDTO>>> GetComments(string userId)
    {
        var comments = await _reviewService.GetUserReviewsAsync(userId);

        if (comments == null || comments.Count == 0)
            return NotFound("У пользователя нет комментариев.");

        return Ok(comments);
    }
}