using WebAPI.Application.DTOs;

namespace WebAPI.Application.Services.Interfaces.AccountDirectoryIServices;

public interface IReviewService
{
    Task<bool> RateAsync(string userId, string reviewId, decimal rating, string? comment);
    Task<(decimal average, int count)> GetAverageAsync(string userId);

    Task<List<UserReviewDTO>> GetUserReviewsAsync(string userId);
}