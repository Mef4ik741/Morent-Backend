namespace WebAPI.Application.DTOs;

public class RateRequestDTO
{
    public string UserId { get; set; }
    public string ReviewerId { get; set; }
    public decimal Rating { get; set; } // 0..5, шаг 0.5
    public string? Comment { get; set; }
}
