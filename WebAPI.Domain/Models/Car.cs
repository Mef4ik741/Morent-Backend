using System.Text.Json;

namespace WebAPI.Domain.Models;

public class Car
{
    private List<string> _imageUrls = new();
    private string _imageUrlsJson = "[]";

    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    public string Name { get; set; } = null!;
    
    public string Brand { get; set; } = null!;
    
    public string Model { get; set; } = null!;
    
    public string Category { get; set; }
    
    public int Year { get; set; }
    
    public decimal Price { get; set; }
    
    public string Description { get; set; } = null!;
    
    public string Location { get; set; }
    
    // Главное изображение (для обратной совместимости)
    public string? ImageUrl { get; set; }
    
    // JSON-строка для хранения списка URL изображений
    public string ImageUrlsJson 
    { 
        get => _imageUrlsJson;
        set 
        {
            _imageUrlsJson = value;
            _imageUrls = string.IsNullOrEmpty(value) 
                ? new List<string>() 
                : JsonSerializer.Deserialize<List<string>>(value) ?? new List<string>();
        }
    }

    // Свойство только для чтения для удобной работы со списком URL
    public List<string> ImageUrls 
    { 
        get => _imageUrls;
        set
        {
            _imageUrls = value ?? new List<string>();
            _imageUrlsJson = JsonSerializer.Serialize(value);
        }
    }
    
    public int RentCount { get; set; }
    
    public string? OwnerUserId { get; set; }
    public string? OwnerUsername { get; set; }
    
    // Навигационное свойство для связи с бронированиями
    public virtual ICollection<CarBooking> Bookings { get; set; } = new List<CarBooking>();
    
    // Конструктор для инициализации
    public Car()
    {
        ImageUrls = new List<string>();
    }
}