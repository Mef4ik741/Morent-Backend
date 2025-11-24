using Microsoft.AspNetCore.Http;
using WebAPI.Application.DTOs;

namespace WebAPI.Application.Services.Interfaces.BusinessLogicIServices;

public interface ICarsService
{
    Task<IEnumerable<CarResponseDTO>> GetAllCarsAsync();
    
    Task<CarResponseDTO?> GetCarByIdAsync(string id);
    
    Task<CarResponseDTO> CreateCarAsync(AddedCarsDTO carDto);
    
    Task<CarResponseDTO?> UpdateCarAsync(string id, UpdateCarsDTO carDto);
    
    Task<bool> DeleteCarAsync(string id);
    
    Task<IEnumerable<CarResponseDTO>> GetCarsByBrandAsync(string brand);
    
    Task<IEnumerable<CarResponseDTO>> GetCarsByYearAsync(int year);
    
    Task<IEnumerable<CarResponseDTO>> GetAvailableCarsAsync();
    
    Task<IEnumerable<CarResponseDTO>> GetCarsByPriceRangeAsync(decimal minPrice, decimal maxPrice);
    
    Task<IEnumerable<CarResponseDTO>> GetMyListedCarsForVerifiedAsync(string userId);
    
    Task<string?> UploadCarImageAsync(IFormFile file);
    
    Task<List<string>> UploadCarImagesAsync(List<IFormFile> files);

    Task<List<ListCarsDTO>> GetCarsSearchAsync(string name, int page = 1, int pageSize = 15);

    Task<IEnumerable<CarResponseDTO>> GetCarsByLocationsAsync(List<string> locations);

    Task<IEnumerable<CarResponseDTO>> GetCarsInBakiAsync(int page = 1, int pageSize = 15);
    Task<IEnumerable<CarResponseDTO>> GetCarsInYasamalAsync(int page = 1, int pageSize = 15);
    Task<IEnumerable<CarResponseDTO>> GetCarsInNarimanovAsync(int page = 1, int pageSize = 15);
    Task<IEnumerable<CarResponseDTO>> GetCarsInSahilAsync(int page = 1, int pageSize = 15);
    Task<IEnumerable<CarResponseDTO>> GetCarsInIcheriSeherAsync(int page = 1, int pageSize = 15);

}