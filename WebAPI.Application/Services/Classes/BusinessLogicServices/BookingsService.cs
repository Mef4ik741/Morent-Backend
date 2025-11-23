using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using WebAPI.Application.DTOs;
using WebAPI.Application.Services.Interfaces.AccountDirectoryIServices;
using WebAPI.Application.Services.Interfaces.BusinessLogicIServices;
using WebAPI.Domain.Models;
using WebAPI.Domain.enums;
using WebAPI.Infrastructure.Data.Context;

namespace WebAPI.Application.Services.Classes.BusinessLogicServices;

public class BookingsService : IBookingsService
{
    private readonly Context _context;
    private readonly IRentNotificationService _notificationService;
    private readonly IBalanceService _balanceService;

    public BookingsService(Context context, IRentNotificationService notificationService, IBalanceService balanceService)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
        _balanceService = balanceService ?? throw new ArgumentNullException(nameof(balanceService));
    }

    public async Task<BookingResponseDTO?> GetByIdAsync(string id)
    {
        var booking = await _context.CarBookings
            .Include(b => b.Car)
            .FirstOrDefaultAsync(b => b.Id == id);
        return booking != null ? Map(booking) : null;
    }

    public async Task<IEnumerable<BookingResponseDTO>> GetByUserAsync(string userId)
    {
        var list = await _context.CarBookings
            .Include(b => b.Car)
            .Where(b => b.RenterUserId == userId)
            .OrderByDescending(b => b.StartDate)
            .ToListAsync();
        return list.Select(Map);
    }

    public async Task<IEnumerable<BookingResponseDTO>> GetByCarAsync(string carId)
    {
        var list = await _context.CarBookings
            .Include(b => b.Car)
            .Where(b => b.CarId == carId)
            .OrderByDescending(b => b.StartDate)
            .ToListAsync();
        return list.Select(Map);
    }

    public async Task<BookingResponseDTO> CreateAsync(string renterUserId, CreateBookingDTO dto)
    {
        if (!await IsCarAvailableAsync(dto.CarId, dto.StartDate, dto.EndDate))
        {
            throw new InvalidOperationException("Автомобиль недоступен для бронирования в указанный период");
        }
        var car = await _context.Cars.FindAsync(dto.CarId);
        if (car == null)
        {
            throw new InvalidOperationException("Автомобиль не найден");
        }

        if (!string.IsNullOrEmpty(car.OwnerUserId) && car.OwnerUserId == renterUserId)
        {
            throw new InvalidOperationException("Нельзя бронировать собственный автомобиль");
        }

        var days = (dto.EndDate - dto.StartDate).Days;
        if (days <= 0) days = 1;

        var totalPrice = car.Price * days;

        var booking = new CarBooking
        {
            Id = Guid.NewGuid().ToString(),
            CarId = dto.CarId,
            RenterUserId = renterUserId,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            Agreement = false,
            StatusActive = true,
            TotalPrice = totalPrice,
            Locations = dto.Locations ?? new List<string>()
        };

        _context.CarBookings.Add(booking);
        car.RentCount += 1;
        await _context.SaveChangesAsync();

        var created = await _context.CarBookings.
            Include(b => b.Car).
            FirstAsync(b => b.Id == booking.Id);
        
        if (!string.IsNullOrEmpty(car.OwnerUserId))
        {
            try
            {
                await _notificationService.SendRentRequestNotificationAsync(
                    booking.Id,
                    car.OwnerUserId,
                    renterUserId
                );
            }
            catch (Exception)
            {
                throw new InvalidOperationException("Не удалось отправить уведомление");
            }
        }
        
        return Map(created);
    }

    public async Task<bool> CancelAsync(string id)
    {
        var booking = await _context.CarBookings.FindAsync(id);
        if (booking == null) return false;
        _context.CarBookings.Remove(booking);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> IsCarAvailableAsync(string carId, DateTime startDate, DateTime endDate, string? excludeId = null)
    {
        var overlappingBookings = await _context.CarBookings
            .Where(b => b.CarId == carId && b.StatusActive && (excludeId == null || b.Id != excludeId) &&
                        ((b.StartDate <= startDate && b.EndDate >= startDate) ||
                         (b.StartDate <= endDate && b.EndDate >= endDate) ||
                         (b.StartDate >= startDate && b.EndDate <= endDate)))
            .AnyAsync();

        return !overlappingBookings;
    }

    public async Task<IEnumerable<BookingResponseDTO>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        var list = await _context.CarBookings
            .Include(b => b.Car)
            .Where(b => b.StartDate >= startDate && b.EndDate <= endDate)
            .OrderBy(b => b.StartDate)
            .ToListAsync();
        return list.Select(Map);
    }

    public async Task<IEnumerable<BookingResponseDTO>> GetOwnerRequestsAsync(string ownerUserId)
    {
        if (string.IsNullOrWhiteSpace(ownerUserId))
        {
            return Enumerable.Empty<BookingResponseDTO>();
        }

        var list = await _context.CarBookings
            .Include(b => b.Car)
            .Where(b => b.Car.OwnerUserId == ownerUserId && b.StatusActive && !b.Agreement)
            .OrderByDescending(b => b.StartDate)
            .ToListAsync();

        return list.Select(Map);
    }

    public async Task<UserBriefDTO?> GetOwnerBriefAsync(string ownerUserId)
    {
        if (string.IsNullOrWhiteSpace(ownerUserId)) return null;

        var brief = await _context.Users
            .AsNoTracking()
            .Where(u => u.Id == ownerUserId)
            .Select(u => new UserBriefDTO(u.Username, u.ImageProfileURL))
            .FirstOrDefaultAsync();

        return brief;
    }

    public async Task<IEnumerable<TopBookedCarDTO>> GetTop10BookedCarsAsync()
    {
        var result = await _context.Cars
            .AsNoTracking()
            .OrderByDescending(c => c.RentCount)
            .ThenBy(c => c.Brand)
            .Take(10)
            .Select(c => new TopBookedCarDTO(c.Id, c.ImageUrl, c.Name, c.Brand, c.Model, c.Location, c.RentCount))
            .ToListAsync();

        return result;
    }
    
    public async Task<IEnumerable<CarImageDTO>?> GetImagesForBookingAsync(string idOrCarId)
    {
        var booking = await _context.CarBookings
            .AsNoTracking()
            .Include(b => b.Car)
            .FirstOrDefaultAsync(b => b.Id == idOrCarId);

        Car? car;
    
        if (booking?.Car != null)
        {
            car = booking.Car;
        }
        else
        {
            car = await _context.Cars
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == idOrCarId);
        }

        if (car == null)
        {
            return null;
        }
        
        List<string> urls;
        try
        {
            urls = string.IsNullOrEmpty(car.ImageUrlsJson) 
                ? new List<string>() 
                : JsonSerializer.Deserialize<List<string>>(car.ImageUrlsJson) ?? new List<string>();
        }
        catch (JsonException ex)
        {
            urls = new List<string>();
        }
        
        if (urls.Count == 0)
            return new List<CarImageDTO>();

        var images = urls
            .Select((url, index) => new CarImageDTO(
                Guid.NewGuid().ToString(),
                url,
                index == 0,
                index))
            .ToList();

        return images;
    }

    public async Task<RentNotificationDto?> RespondToRentRequestAsync(string bookingId, string ownerUserId, bool isApproved, string? message)
    {
        return await _notificationService.RespondToRentRequestAsync(bookingId, ownerUserId, isApproved, message);
    }

    private static BookingResponseDTO Map(CarBooking b)
    {
        return new BookingResponseDTO(
            b.Id,
            b.RenterUserId,
            b.CarId,
            b.Car.Name,
            b.Car.Brand,
            b.StartDate,
            b.EndDate,
            b.TotalPrice,
            b.Agreement,
            b.StatusActive,
            b.Locations
        );
    }
}
