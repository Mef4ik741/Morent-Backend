using Microsoft.EntityFrameworkCore;
using WebAPI.Infrastructure.Data.Context;

namespace WebAPI.Application.Utils;

public static class UtilsClass
{
    public static async Task<List<string>> GetCurrentlyBookedCarIdsAsync(Context context, DateTime now, List<string> ids)
    {
        return await context.CarBookings
            .Where(b => ids.Contains(b.CarId) && b.StatusActive && b.StartDate <= now && b.EndDate >= now)
            .Select(b => b.CarId)
            .Distinct()
            .ToListAsync();
    }
    
    public static async Task<bool> HasActiveBookingAsync(Context context, string id, DateTime now)
    {
        return await context.CarBookings
            .AnyAsync(b => b.CarId == id && b.StatusActive && b.StartDate <= now && b.EndDate >= now);
    }
}