using WebAPI.Application.DTOs;

namespace WebAPI.Application.Services.Interfaces.BusinessLogicIServices;

public interface IFavoritesService
{
    Task<bool> AddToFavoritesAsync(string userId, string carId);
    Task<bool> RemoveFromFavoritesAsync(string userId, string carId);
    Task<IEnumerable<FavoriteCarDTO>> GetFavoritesAsync(string userId);
}
