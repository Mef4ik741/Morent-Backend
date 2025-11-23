using WebAPI.Application.DTOs;

namespace WebAPI.Application.Services.Interfaces.BusinessLogicIServices;

public interface IBookingsService
{
    Task<BookingResponseDTO?> GetByIdAsync(string id);
    Task<IEnumerable<BookingResponseDTO>> GetByUserAsync(string userId);
    Task<IEnumerable<BookingResponseDTO>> GetByCarAsync(string carId);
    Task<BookingResponseDTO> CreateAsync(string renterUserId, CreateBookingDTO dto);
    Task<bool> CancelAsync(string id);
    Task<bool> IsCarAvailableAsync(string carId, DateTime startDate, DateTime endDate, string? excludeId = null);
    Task<IEnumerable<BookingResponseDTO>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);
    Task<UserBriefDTO?> GetOwnerBriefAsync(string ownerUserId);
    Task<IEnumerable<TopBookedCarDTO>> GetTop10BookedCarsAsync();
    
    Task<IEnumerable<CarImageDTO>?> GetImagesForBookingAsync(string bookingId);
}