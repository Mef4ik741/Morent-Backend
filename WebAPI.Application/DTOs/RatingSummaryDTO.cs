namespace WebAPI.Application.DTOs;

public record RatingSummaryDTO(string UserId, decimal AverageRating, int ReviewsCount);
