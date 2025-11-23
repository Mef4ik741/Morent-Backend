using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using System.Text.Json;
using WebAPI.Application.DTOs;
using WebAPI.Application.Services.Interfaces.BusinessLogicIServices;
using WebAPI.Domain.Models;
using WebAPI.Infrastructure.Data.Context;

namespace WebAPI.Application.Services.Classes.BusinessLogicServices;

public class FavoritesService : IFavoritesService
{
    private readonly Context _context;

    public FavoritesService(Context context)
    {
        _context = context;
    }

    public async Task<bool> AddToFavoritesAsync(string userId, string carId)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(carId)) { return false; }

        var carExists = await _context.Cars.AnyAsync(c => c.Id == carId);
        if (!carExists)
            return false;

        var exists = await _context.Favorites.AnyAsync(f => f.UserId == userId && f.CarId == carId);
        if (exists) { return true; }

        var fav = new Favorites
        {
            Id = Guid.NewGuid().ToString(),
            UserId = userId,
            CarId = carId
        };
        _context.Favorites.Add(fav);
        try
        {
            await _context.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException sqlEx && (sqlEx.Number == 2601 || sqlEx.Number == 2627))
        {
            return true;
        }
    }

    public async Task<bool> RemoveFromFavoritesAsync(string userId, string carId)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(carId)){ return false; }

        var fav = await _context.Favorites.FirstOrDefaultAsync(f => f.UserId == userId && f.CarId == carId);
        if (fav == null)
            return false;

        _context.Favorites.Remove(fav);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<FavoriteCarDTO>> GetFavoritesAsync(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId)){ return Enumerable.Empty<FavoriteCarDTO>(); }

        var rows = await _context.
            Favorites
            .Where(f => f.UserId == userId)
            .Join(
                _context.Cars,
                f => f.CarId,
                c => c.Id,
                (f, c) => new
                {
                    c.Id,
                    c.Name,
                    c.Brand,
                    c.Model,
                    c.Price,
                    c.Location,
                    c.ImageUrlsJson,
                    c.ImageUrl
                }
            )
            .ToListAsync();

        var result = rows.Select(x =>
        {
            string? primary = null;
            if (!string.IsNullOrEmpty(x.ImageUrlsJson) && x.ImageUrlsJson != "[]")
            {
                try
                {
                    var list = JsonSerializer.Deserialize<List<string>>(x.ImageUrlsJson) ?? new List<string>();
                    primary = list.FirstOrDefault();
                }
                catch
                {
                    primary = null;
                }
            }
            primary ??= x.ImageUrl;

            var name = string.IsNullOrWhiteSpace(x.Name) ? $"{x.Brand} {x.Model}" : x.Name;

            return new FavoriteCarDTO(
                x.Id,
                name,
                x.Price,
                x.Location,
                primary
            );
        }).ToList();

        return result;
    }
}