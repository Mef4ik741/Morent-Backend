using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using WebAPI.Application.Cloudinary;
using WebAPI.Application.DTOs;
using WebAPI.Application.Services.Interfaces.BusinessLogicIServices;
using WebAPI.Application.Utils;
using WebAPI.Domain.Models;
using WebAPI.Infrastructure.Data.Context;

namespace WebAPI.Application.Services.Classes.BusinessLogicServices;

public class CarsService : ICarsService
{
    private readonly Context _context;
    private readonly ICloudinaryService _cloudinaryService;

    public CarsService(Context context, ICloudinaryService cloudinaryService)
    {
        _context = context;
        _cloudinaryService = cloudinaryService;
    }

    public async Task<IEnumerable<CarResponseDTO>> GetAllCarsAsync()
    {
        var cars = await _context.Cars.ToListAsync();

        LoadImageUrls(cars);

        var now = DateTime.UtcNow;
        var ids = cars.Select(c => c.Id).ToList();
        var bookedIds = await UtilsClass.GetCurrentlyBookedCarIdsAsync(_context, now, ids);

        var result = cars.Select(c => MapToResponseDTO(c, !bookedIds.Contains(c.Id))).ToList();
        
        return result;
    }

    public async Task<CarResponseDTO?> GetCarByIdAsync(string id)
    {
        var car = await _context.Cars.FindAsync(id);

        if (car == null)
        {
            return null;
        }

        LoadImageUrls(car);

        var now = DateTime.UtcNow;
        var hasActiveBooking = await UtilsClass.HasActiveBookingAsync(_context, id, now);

        return MapToResponseDTO(car, !hasActiveBooking);
    }

    public async Task<CarResponseDTO> CreateCarAsync(AddedCarsDTO carDto)
    {
        var primaryUrl = carDto.PrimaryImageUrl ?? carDto.ImageUrl;
        var imageUrls = new List<string>();

        if (!string.IsNullOrWhiteSpace(primaryUrl))
        {
            imageUrls.Add(primaryUrl);
        }

        if (carDto.GalleryImageUrls != null)
        {
            foreach (var url in carDto.GalleryImageUrls.Distinct())
            {
                if (!string.Equals(url, primaryUrl, StringComparison.OrdinalIgnoreCase))
                {
                    imageUrls.Add(url);
                }
            }
        }

        var car = new Car
        {
            Id = Guid.NewGuid().ToString(),
            Name = carDto.Name, 
            Brand = carDto.Brand,
            Model = carDto.Model,
            Year = carDto.Year,
            Price = carDto.Price,
            Description = carDto.Description,
            Location = carDto.Location,
            OwnerUserId = carDto.OwnerUserId,
            ImageUrl = primaryUrl,
            ImageUrls = imageUrls
        };

        _context.Cars.Add(car);
        await _context.SaveChangesAsync();
        
        return MapToResponseDTO(car, true);
    }

    public async Task<CarResponseDTO?> UpdateCarAsync(string id, UpdateCarsDTO carDto)
    {
        var existingCar = await _context.Cars.FindAsync(id);
        if (existingCar == null)
        {
            return null;
        }

        existingCar.Name = carDto.Name;
        existingCar.Brand = carDto.Brand;
        existingCar.Model = carDto.Model;
        existingCar.Year = carDto.Year;
        existingCar.Price = carDto.Price;
        existingCar.Description = carDto.Description;
        existingCar.Location = carDto.Location;
        
        if (carDto.PrimaryImageUrl != null || carDto.GalleryImageUrls != null || carDto.ImageUrl != null)
        {
            var primaryUrl = carDto.PrimaryImageUrl ?? carDto.ImageUrl;
            var imageUrls = new List<string>();

            if (!string.IsNullOrWhiteSpace(primaryUrl))
            {
                imageUrls.Add(primaryUrl);
                existingCar.ImageUrl = primaryUrl;
            }

            if (carDto.GalleryImageUrls != null)
            {
                foreach (var url in carDto.GalleryImageUrls.Distinct())
                {
                    if (!string.Equals(url, primaryUrl, StringComparison.OrdinalIgnoreCase))
                    {
                        imageUrls.Add(url);
                    }
                }
            }

            existingCar.ImageUrls = imageUrls;
        }

        await _context.SaveChangesAsync();
        
        var now = DateTime.UtcNow;
        var hasActiveBooking = await UtilsClass.HasActiveBookingAsync(_context, id, now);
            
        return MapToResponseDTO(existingCar, !hasActiveBooking);
    }

    public async Task<bool> DeleteCarAsync(string id)
    {
        var car = await _context.Cars.FindAsync(id);
        if (car == null)
        {
            return false;
        }

        var hasActiveBookings = await _context.CarBookings
            .AnyAsync(b => b.CarId == id && b.StatusActive);

        if (hasActiveBookings)
        {
            return false;
        }

        _context.Cars.Remove(car);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<CarResponseDTO>> GetCarsByBrandAsync(string brand)
    {
        var cars = await _context.Cars
            .Where(c => c.Brand.ToLower().Contains(brand.ToLower()))
            .ToListAsync();

        LoadImageUrls(cars);

        var now = DateTime.UtcNow;
        var ids = cars.Select(c => c.Id).ToList();
        var bookedIds = await UtilsClass.GetCurrentlyBookedCarIdsAsync(_context, now, ids);

        return cars.Select(c => MapToResponseDTO(c, !bookedIds.Contains(c.Id)));
    }

    public async Task<IEnumerable<CarResponseDTO>> GetCarsByYearAsync(int year)
    {
        var cars = await _context.Cars
            .Where(c => c.Year == year)
            .ToListAsync();

        LoadImageUrls(cars);

        var now = DateTime.UtcNow;
        var ids = cars.Select(c => c.Id).ToList();
        var bookedIds = await UtilsClass.GetCurrentlyBookedCarIdsAsync(_context, now, ids);

        return cars.Select(c => MapToResponseDTO(c, !bookedIds.Contains(c.Id)));
    }

    public async Task<IEnumerable<CarResponseDTO>> GetAvailableCarsAsync()
    {
        var currentDate = DateTime.UtcNow;

        var cars = await _context.Cars
            .Where(c => !_context.CarBookings.Any(b =>
                b.CarId == c.Id &&
                b.StatusActive &&
                b.StartDate <= currentDate &&
                b.EndDate >= currentDate))
            .ToListAsync();

        LoadImageUrls(cars);

        return cars.Select(c => MapToResponseDTO(c, true));
    }

    public async Task<IEnumerable<CarResponseDTO>> GetCarsByPriceRangeAsync(decimal minPrice, decimal maxPrice)
    {
        var cars = await _context.Cars
            .Where(c => c.Price >= minPrice && c.Price <= maxPrice)
            .ToListAsync();

        LoadImageUrls(cars);

        var now = DateTime.UtcNow;
        var ids = cars.Select(c => c.Id).ToList();
        var bookedIds = await UtilsClass.GetCurrentlyBookedCarIdsAsync(_context, now, ids);

        return cars.Select(c => MapToResponseDTO(c, !bookedIds.Contains(c.Id)));
    }

    public async Task<IEnumerable<CarResponseDTO>> GetMyListedCarsForVerifiedAsync(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Enumerable.Empty<CarResponseDTO>();
        }

        var isVerified = await _context.UserRoles
            .Include(ur => ur.Role)
            .AnyAsync(ur => ur.UserId == userId && ur.Role.Name == "UserVerified");

        if (!isVerified)
        {
            return Enumerable.Empty<CarResponseDTO>();
        }

        var myCars = await _context.Cars
            .Where(c => c.OwnerUserId == userId)
            .ToListAsync();

        LoadImageUrls(myCars);

        var now = DateTime.UtcNow;
        var ids = myCars.Select(c => c.Id).ToList();
        var bookedIds = await UtilsClass.GetCurrentlyBookedCarIdsAsync(_context, now, ids);

        return myCars.Select(c => MapToResponseDTO(c, !bookedIds.Contains(c.Id)));
    }

    public async Task<List<ListCarsDTO>> GetCarsSearchAsync(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return new List<ListCarsDTO>();

        var cars = await _context.Cars
            .Where(c => EF.Functions.Like(c.Location.ToLower(), $"%{name.ToLower()}%"))
            .ToListAsync();

        LoadImageUrls(cars);

        return cars.Select(c => new ListCarsDTO(
            c.Id,
            c.ImageUrl ?? c.ImageUrls.FirstOrDefault(),
            c.Name,
            c.OwnerUsername,
            c.Location,
            c.Price
        )).ToList();
    }

    public async Task<IEnumerable<CarResponseDTO>> GetCarsByLocationsAsync(List<string> locations)
    {
        if (locations == null || locations.Count == 0)
        {
            return Enumerable.Empty<CarResponseDTO>();
        }

        var normalizedLocations = locations
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .Select(l => l.Trim().ToLower())
            .Distinct()
            .ToList();

        if (normalizedLocations.Count == 0)
        {
            return Enumerable.Empty<CarResponseDTO>();
        }

        var cars = await _context.Cars
            .Where(c => normalizedLocations.Contains(c.Location.ToLower()))
            .ToListAsync();

        LoadImageUrls(cars);

        var now = DateTime.UtcNow;
        var ids = cars.Select(c => c.Id).ToList();
        var bookedIds = await UtilsClass.GetCurrentlyBookedCarIdsAsync(_context, now, ids);

        return cars.Select(c => MapToResponseDTO(c, !bookedIds.Contains(c.Id)));
    }

    public async Task<IEnumerable<CarResponseDTO>> GetCarsInBakiAsync(int page = 1, int pageSize = 15)
    {
        return await GetCarsByLocationsAsync(new List<string> { "Baki" }, page, pageSize);  
    }

    public async Task<IEnumerable<CarResponseDTO>> GetCarsInYasamalAsync(int page = 1, int pageSize = 15)
    {
        return await GetCarsByLocationsAsync(new List<string> { "Yasamal" }, page, pageSize);
    }

    public async Task<IEnumerable<CarResponseDTO>> GetCarsInNarimanovAsync(int page = 1, int pageSize = 15)
    {
        return await GetCarsByLocationsAsync(new List<string> { "Narimanov" }, page, pageSize);
    }

    public async Task<IEnumerable<CarResponseDTO>> GetCarsInSahilAsync(int page = 1, int pageSize = 15)
    {
        return await GetCarsByLocationsAsync(new List<string> { "Sahil" }, page, pageSize);
    }

    private async Task<IEnumerable<CarResponseDTO>> GetCarsByLocationsAsync(
        List<string> locations,
        int page,
        int pageSize)
    {
        if (page <= 0) page = 1;
        if (pageSize <= 0) pageSize = 15;
    
        var lowerLocations = locations
            .Select(l => l.Trim().ToLower())
            .ToList();
    
        var query = _context.Cars
            .Where(c => lowerLocations.Contains(c.Location.ToLower()));
    
        return await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new CarResponseDTO(
                c.Id,
                c.Name,
                c.Brand,
                c.Model,
                c.Year,
                c.Price,
                c.Description,
                c.Location,
                true,                          // IsAvailable — позже можно заменить
                c.ImageUrl,
                c.OwnerUserId,
                c.ImageUrls
                    .Select((url, index) => new CarImageDTO(
                        Guid.NewGuid().ToString(),
                        url,
                        index == 0,
                        index
                    ))
                    .ToList()
            ))
            .ToListAsync();
    }

    
    public async Task<IEnumerable<CarResponseDTO>> GetCarsInIcheriSeherAsync(int page = 1, int pageSize = 15)
    {
        return await GetCarsByLocationsAsync(new List<string> { "Icheri Seher" }, page, pageSize);
    }

    private void LoadImageUrls(Car car)
    {
        if (!string.IsNullOrEmpty(car.ImageUrlsJson))
        {
            car.ImageUrls = JsonSerializer.Deserialize<List<string>>(car.ImageUrlsJson) ?? new List<string>();
        }
    }

    private void LoadImageUrls(IEnumerable<Car> cars)
    {
        foreach (var car in cars)
        {
            LoadImageUrls(car);
        }
    }

    private CarResponseDTO MapToResponseDTO(Car car, bool isAvailable)
    {
        var primaryUrl = car.ImageUrl ?? car.ImageUrls?.FirstOrDefault();
        
        var imagesDto = car.ImageUrls?
            .Select((url, index) => new CarImageDTO(
                Guid.NewGuid().ToString(),
                url,
                index == 0, 
                index
            ))
            .ToList() ?? new List<CarImageDTO>();
        
        return new CarResponseDTO(
            car.Id,
            car.Name,
            car.Brand,
            car.Model,
            car.Year,
            car.Price,
            car.Description,
            car.Location,
            isAvailable,
            primaryUrl,
            car.OwnerUserId ?? null!,
            imagesDto
        );
    }

    public async Task<string?> UploadCarImageAsync(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return null;

        await using var stream = file.OpenReadStream();
        return await _cloudinaryService.UploadAsync(stream, file.FileName);
    }

    public async Task<List<string>> UploadCarImagesAsync(List<IFormFile> files)
    {
        var urls = new List<string>();
        
        if (files == null || files.Count == 0)
            return urls;

        foreach (var file in files)
        {
            if (file == null || file.Length == 0) 
                continue;
                
            await using var stream = file.OpenReadStream();
            var url = await _cloudinaryService.UploadAsync(stream, file.FileName);
            
            if (!string.IsNullOrEmpty(url))
                urls.Add(url);
        }

        return urls;
    }
}