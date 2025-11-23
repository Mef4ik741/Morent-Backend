using Microsoft.EntityFrameworkCore;
using WebAPI.Application.DTOs;
using WebAPI.Domain.Models;
using WebAPI.Infrastructure.Data.Context;
using WebAPI.Application.Services.Interfaces.AccountDirectoryIServices;
using WebAPI.Domain.enums;

namespace WebAPI.Application.Services.Classes.AccountDirectoryServices;

public class ReviewService : IReviewService
{
    private readonly Context _context;
    public ReviewService(Context context)
    {
        _context = context;
    }

    public async Task<bool> RateAsync(string userId, string reviewerId, decimal rating, string? comment)
    {
        if (rating < 0m || rating > 5m) return false;
        var step = (rating * 10) % 5;
        if (step != 0) return false;

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null) return false;

        if (userId == reviewerId) return false;

        var hasRental = await _context.CarBookings
            .Include(b => b.Car)
            .AnyAsync(b => b.RenterUserId == reviewerId && b.Car.OwnerUserId == userId);

        if (!hasRental) return false;

        var existing = await _context.Reviews
            .FirstOrDefaultAsync(r => r.UserId == userId && r.ReviewerId == reviewerId);

        if (existing == null)
        {
            _context.Reviews.Add(new Review
            {
                UserId = userId,
                ReviewerId = reviewerId,
                Rating = rating,
                Comment = comment
            });
        }
        else
        {
            existing.Rating = rating;
            existing.Comment = comment;
        }

        await _context.SaveChangesAsync();

        await RecalculateUserAggregatesAndRank(user);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<(decimal average, int count)> GetAverageAsync(string userId)
    {
        var query = _context.Reviews.AsNoTracking().Where(r => r.UserId == userId);
        var count = await query.CountAsync();
        if (count == 0) return (0m, 0);
        var avg = await query.AverageAsync(r => r.Rating);
        avg = Math.Round(avg, 1, MidpointRounding.AwayFromZero);
        return (avg, count);
    }
    
    public async Task<List<UserReviewDTO>> GetUserReviewsAsync(string userId)
    {
        var reviews = await _context.Reviews
            .AsNoTracking()
            .Where(r => r.UserId == userId && 
                        r.Comment != null && 
                        r.Comment.Trim() != "")
            .OrderByDescending(r => r.Id)
            .Select(r => new UserReviewDTO(
                r.ReviewerId,
                r.Reviewer != null ? r.Reviewer.Username : null,
                r.Rating,
                r.Comment,
                r.CreatedAt
            ))
            .ToListAsync();

        return reviews;
    }

    private async Task RecalculateUserAggregatesAndRank(User user)
    {
        var userId = user.Id;
        var reviews = _context.Reviews.Where(r => r.UserId == userId);
        var count = await reviews.CountAsync();
        var negative = await reviews.CountAsync(r => r.Rating < 3m);
        var avg = count == 0 ? 0m : await reviews.AverageAsync(r => r.Rating);

        user.ReviewCount = count;
        user.NegativeReviewCount = negative;

        UpdateRankByStars(user, avg, count);
    }
    
    private void UpdateRankByStars(User user, decimal avg, int count)
    {
        if (count >= 20 && avg <= 1.0m)
        {
            user.Rank = UserRank.Banned;
            return;
        }
        if (count >= 10 && avg < 2.0m)
        {
            user.Rank = UserRank.Fraudulent;
            return;
        }

        if (!user.IsVerified)
        {
            user.Rank = UserRank.Beginner;
            return;
        }

        if (count >= 1000 && avg >= 4.8m) { user.Rank = UserRank.Legendary; return; }
        if (count >= 700  && avg >= 4.7m) { user.Rank = UserRank.Ambassador; return; }
        if (count >= 400  && avg >= 4.6m) { user.Rank = UserRank.Veteran; return; }
        if (count >= 200  && avg >= 4.5m) { user.Rank = UserRank.Distinguished; return; }
        if (count >= 100  && avg >= 4.4m) { user.Rank = UserRank.Elite; return; }
        if (count >= 50   && avg >= 4.2m) { user.Rank = UserRank.Endorsed; return; }
        if (count >= 25   && avg >= 4.0m) { user.Rank = UserRank.Respected; return; }
        if (count >= 10   && avg >= 3.5m) { user.Rank = UserRank.Reliable; return; }
        if (count >= 3    && avg >= 3.0m) { user.Rank = UserRank.Trusted; return; }

        user.Rank = UserRank.Verified;
    }
}
