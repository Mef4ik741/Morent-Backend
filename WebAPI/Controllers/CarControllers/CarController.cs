using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using WebAPI.Application.DTOs;
using WebAPI.Application.Services.Interfaces.BusinessLogicIServices;

namespace WebAPI.Controllers.CarControllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CarController : ControllerBase
{
    private readonly ICarsService _carsService;

    public CarController(ICarsService carsService)
    {
        _carsService = carsService;
    }

    [HttpPost("add")]
    [Authorize(Roles = "AppAdmin,AppSuperAdmin,UserVerified")]
    public async Task<IActionResult> CreateCarAsync([FromBody] AddedCarsDTO carDto)
    {
        try
        {
            if (carDto == null)
            {
                return BadRequest("Данные автомобиля не могут быть пустыми");
            }

            var createdCar = await _carsService.CreateCarAsync(carDto);
            return Ok(createdCar);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Ошибка при создании автомобиля: {ex.Message}");
        }
    }

    [HttpGet("all")]
    [AllowAnonymous]
    public async Task<IActionResult> GetAllCars()
    {
        try
        {
            var cars = await _carsService.GetAllCarsAsync();
            return Ok(cars);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Ошибка при получении списка автомобилей: {ex.Message}");
        }
    }

    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetCarByIdAsync(string id)
    {
        try
        {
            var car = await _carsService.GetCarByIdAsync(id);
            if (car == null)
            {
                return NotFound($"Автомобиль с ID {id} не найден");
            }
            return Ok(car);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Ошибка при получении автомобиля: {ex.Message}");
        }
    }
    
    [HttpDelete("{id}")]
    [Authorize(Roles = "AppAdmin,AppSuperAdmin,UserVerified")]
    public async Task<IActionResult> DeleteCarByIdAsync(string id)
    {
        try
        {
            var result = await _carsService.DeleteCarAsync(id);
            if (!result)
            {
                return NotFound($"Автомобиль с ID {id} не найден или имеет активные аренды");
            }
            return Ok($"Автомобиль с ID {id} успешно удален");
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Ошибка при удалении автомобиля: {ex.Message}");
        }
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "AppAdmin,AppSuperAdmin,UserVerified")] 
    public async Task<IActionResult> UpdateCarByIdAsync(string id, [FromBody] UpdateCarsDTO carDto)
    {
        try
        {
            if (carDto == null)
            {
                return BadRequest("Данные автомобиля не могут быть пустыми");
            }

            var updatedCar = await _carsService.UpdateCarAsync(id, carDto);
            if (updatedCar == null)
            {
                return NotFound($"Автомобиль с ID {id} не найден");
            }
            return Ok(updatedCar);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Ошибка при обновлении автомобиля: {ex.Message}");
        }
    }

    [HttpGet("brand/{brand}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetCarsByBrand(string brand)
    {
        try
        {
            var cars = await _carsService.GetCarsByBrandAsync(brand);
            return Ok(cars);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Ошибка при поиске автомобилей по бренду: {ex.Message}");
        }
    }

    [HttpGet("year/{year}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetCarsByYear(int year)
    {
        try
        {
            var cars = await _carsService.GetCarsByYearAsync(year);
            return Ok(cars);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Ошибка при поиске автомобилей по году: {ex.Message}");
        }
    }

    [HttpGet("available")]
    [AllowAnonymous]
    public async Task<IActionResult> GetAvailableCars()
    {
        try
        {
            var cars = await _carsService.GetAvailableCarsAsync();
            return Ok(cars);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Ошибка при получении доступных автомобилей: {ex.Message}");
        }
    }

    [HttpGet("price")]
    [AllowAnonymous]
    public async Task<IActionResult> GetCarsByPriceRange([FromQuery] decimal minPrice, [FromQuery] decimal maxPrice)
    {
        try
        {
            var cars = await _carsService.GetCarsByPriceRangeAsync(minPrice, maxPrice);
            return Ok(cars);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Ошибка при поиске автомобилей по цене: {ex.Message}");
        }
    }

    [HttpPost("upload-image")]
    [AllowAnonymous]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadCarImage(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("Файл не выбран");
        }
        
        var url = await _carsService.UploadCarImageAsync(file);

        if (string.IsNullOrEmpty(url))
        {
            return StatusCode(500, "Ошибка загрузки в Cloudinary");
        }
            
        return Ok(new { imageUrl = url });
    }

    [HttpPost("upload-images")]
    [AllowAnonymous]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadCarImages(List<IFormFile> files)
    {
        if (files == null || files.Count == 0)
        {
            return BadRequest("Файлы не выбраны");
        }
        
        var urls = await _carsService.UploadCarImagesAsync(files);

        if (urls.Count == 0)
        {
            return StatusCode(500, "Не удалось загрузить ни один файл в Cloudinary");
        }
        
        return Ok(new { imageUrls = urls });
    }

    [HttpGet("my-listed")]
    [Authorize(Roles = "UserVerified")] 
    public async Task<IActionResult> GetMyListedCars()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("id")?.Value;
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized("Не удалось определить пользователя из токена");
        }

        var cars = await _carsService.GetMyListedCarsForVerifiedAsync(userId);
        return Ok(cars);
    }

    [HttpGet("CarsSearched")]
    [AllowAnonymous]
    public async Task<IActionResult> GetListCarsSearch([FromQuery] string name, [FromQuery] int page = 1, [FromQuery] int pageSize = 15)
    {
        var cars = await _carsService.GetCarsSearchAsync(name, page, pageSize);
        return Ok(cars);
    }

    [HttpGet("location/baki")]
    [AllowAnonymous]
    public async Task<IActionResult> GetCarsInBaki([FromQuery] int page = 1, [FromQuery] int pageSize = 15)
    {
        try
        {
            var cars = await _carsService.GetCarsInBakiAsync(page, pageSize);
            return Ok(cars);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Ошибка при получении автомобилей в Baki: {ex.Message}");
        }
    }
    
    [HttpGet("location/yasamal")]
    [AllowAnonymous]
    public async Task<IActionResult> GetCarsInYasamal([FromQuery] int page = 1, [FromQuery] int pageSize = 15)
    {
        try
        {
            var cars = await _carsService.GetCarsInYasamalAsync(page, pageSize);
            return Ok(cars);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Ошибка при получении автомобилей в Yasamal: {ex.Message}");
        }
    }
    
    [HttpGet("location/narimanov")]
    [AllowAnonymous]
    public async Task<IActionResult> GetCarsInNarimanov([FromQuery] int page = 1, [FromQuery] int pageSize = 15)
    {
        try
        {
            var cars = await _carsService.GetCarsInNarimanovAsync(page, pageSize);
            return Ok(cars);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Ошибка при получении автомобилей в Narimanov: {ex.Message}");
        }
    }
    
    [HttpGet("location/sahil")]
    [AllowAnonymous]
    public async Task<IActionResult> GetCarsInSahil([FromQuery] int page = 1, [FromQuery] int pageSize = 15)
    {
        try
        {
            var cars = await _carsService.GetCarsInSahilAsync(page, pageSize);
            return Ok(cars);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Ошибка при получении автомобилей в Sahil: {ex.Message}");
        }
    }
    
    [HttpGet("location/icheri-seher")]
    [AllowAnonymous]
    public async Task<IActionResult> GetCarsInIcheriSeher([FromQuery] int page = 1, [FromQuery] int pageSize = 15)
    {
        try
        {
            var cars = await _carsService.GetCarsInIcheriSeherAsync(page, pageSize);
            return Ok(cars);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Ошибка при получении автомобилей в Icheri Seher: {ex.Message}");
        }
    }

}