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

    Task<List<ListCarsDTO>> GetCarsSearchAsync(string name);

    Task<IEnumerable<CarResponseDTO>> GetCarsByLocationsAsync(List<string> locations);

    Task<IEnumerable<CarResponseDTO>> GetCarsInBakiAsync();
    Task<IEnumerable<CarResponseDTO>> GetCarsInYasamalAsync();
    Task<IEnumerable<CarResponseDTO>> GetCarsInNarimanovAsync();
    Task<IEnumerable<CarResponseDTO>> GetCarsInSahilAsync();
    Task<IEnumerable<CarResponseDTO>> GetCarsInIcheriSeherAsync();

}