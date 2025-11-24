namespace WebAPI.Application.DTOs;

public record ListCarsDTO(string Id, string ImageCar, string Name, string OwnerUsername, string Location, decimal Price);