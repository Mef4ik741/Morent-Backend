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
    
    public static string DetectCategory(string brand, string model)
    {
        brand = brand.ToLower();
        model = model.ToLower();
        string full = $"{brand} {model}";

        // ---------- ELECTRIC ----------
        string[] electricKeywords = { "tesla", "ev", "electro", "leaf", "e-tron", "id.", "polestar" };
        if (electricKeywords.Any(k => full.Contains(k)))
            return "Electric";

        // ---------- HYBRID ----------
        string[] hybridKeywords = { "hybrid", "phev", "prius" };
        if (hybridKeywords.Any(k => full.Contains(k)))
            return "Hybrid";

        // ---------- SUV / CROSSOVER ----------
        string[] suvModels = {
            "x1","x2","x3","x4","x5","x6","x7",
            "q3","q5","q7","q8",
            "glc","gle","gls",
            "sportage","seltos","sorento","telluride",
            "rav4","highlander","4runner","tahoe","suburban","bronco"
        };
        if (suvModels.Any(m => model.Contains(m)))
            return "SUV";

        string[] suvKeywords = { "suv", "4x4", "awd", "crossover", "jeep" };
        if (suvKeywords.Any(k => full.Contains(k)))
            return "SUV";

        // ---------- MINIVAN ----------
        string[] minivans = { "sienna", "odyssey", "carnival", "v-class", "voyager" };
        if (minivans.Any(m => full.Contains(m)))
            return "Minivan";

        // ---------- PICKUP ----------
        string[] pickups = { "f-150", "ranger", "hilux", "tundra", "silverado", "ram" };
        if (pickups.Any(m => full.Contains(m)))
            return "Pickup";

        // ---------- PREMIUM ----------
        string[] premiumKeywords = {
            "amg", "m3","m4","m5","rs","s-class","7 series","a8","panamera","taycan"
        };
        if (premiumKeywords.Any(k => full.Contains(k)))
            return "Premium";

        // ---------- COUPE ----------
        string[] coupes = { "mustang", "camaro", "challenger", "supra", "z4", "brz", "gt86" };
        if (coupes.Any(m => full.Contains(m)))
            return "Coupe";

        // ---------- CONVERTIBLE ----------
        string[] convertibleKeywords = { "convertible", "cabrio", "roadster", "spyder" };
        if (convertibleKeywords.Any(k => full.Contains(k)))
            return "Convertible";

        // ---------- HATCHBACK ----------
        string[] hatchbacks = { "golf", "focus", "fiesta", "yaris", "i20", "i30" };
        if (hatchbacks.Any(m => full.Contains(m)))
            return "Hatchback";

        // ---------- UNIVERSAL / WAGON ----------
        string[] wagons = { "touring", "avant", "wagon", "estate", "variant" };
        if (wagons.Any(m => full.Contains(m)))
            return "Wagon";

        // ---------- DEFAULT ----------
        return "Sedan";
    }
}