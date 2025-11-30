using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using WebAPI.Application.Cloudinary;
using WebAPI.Application.DTOs;
using WebAPI.Application.DTOs.Response;
using WebAPI.Application.Services.Interfaces.AccountDirectoryIServices;
using WebAPI.Domain.Models;
using WebAPI.Infrastructure.Data.Context;
using static BCrypt.Net.BCrypt;

namespace WebAPI.Application.Services.Classes.AccountDirectoryServices;

public class AccountService : IAccountService
{
    private readonly Context _context;
    private readonly IEmailService _emailService;
    private readonly ICloudinaryService _cloudinaryService;

    public AccountService(Context context, IEmailService emailService, ICloudinaryService cloudinaryService)
    {
        _context = context;
        _emailService = emailService;
        _cloudinaryService = cloudinaryService;
    }

    public async Task VerifyEmailAsync(string id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user != null)
        {
            user.IsConfirmed = true;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<Result> ConfirmEmailAsync(HttpContext context, ClaimsPrincipal userPrincipal, string token)
    {
        var email = userPrincipal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
        var username = userPrincipal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;
        
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(username))
            return Result.Error("Не удалось получить информацию о пользователе");
            
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null)
            return Result.Error("Пользователь не найден");
        
        var confirmationLink = $"{context.Request.Scheme}://{context.Request.Host}/api/Account/Email/Verify/{user.Id}/{token}";

        try
        {
            await _emailService.SendEmailConfirmationAsync(email, username, confirmationLink);
        }
        catch (Exception ex)
        {
            return Result.Error($"Не удалось отправить письмо подтверждения: {ex.Message}");
        }
        
        return Result.Success("Письмо с подтверждением отправлено");
    }
    
    public async Task<Result> RegisterAsync(RegisterRequestDTO request)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        
        try
        {
            var existingUser = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Email == request.Email);
                
            if (existingUser != null)
                return Result.Error("Пользователь с таким email уже существует");

            var existingUsername = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Username == request.Username);
                
            if (existingUsername != null)
                return Result.Error("Пользователь с таким именем уже существует");
            if (request.Password != request.ConfirmPassword)
                return Result.Error("Пароли не совпадают");
            if (string.IsNullOrEmpty(request.Password) || request.Password.Length < 6)
                return Result.Error("Пароль должен содержать минимум 6 символов");
            
            var user = new User
            {
                Id = Guid.NewGuid().ToString(),
                Name = request.Name,
                Surname = request.Surname,
                Username = request.Username,
                Email = request.Email,
                Password = HashPassword(request.Password),
                CreatedAt = DateTime.UtcNow,
                IsConfirmed = false,
                IsVerified = false,
                Balance = 0,
                Rank = 0,
                ReviewCount = 0,
                NegativeReviewCount = 0
            };

            var defaultRole = await _context.Roles
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Name == "User");
                
            if (defaultRole == null)
            {
                defaultRole = new Role { 
                    Id = Guid.NewGuid().ToString(),
                    Name = "User",
                    CreatedAt = DateTime.UtcNow
                };
                
                _context.Roles.Add(defaultRole);
                await _context.SaveChangesAsync();
                
                _context.Entry(defaultRole).State = EntityState.Detached;
            }

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var userRole = new UserRole
            {
                UserId = user.Id,
                RoleId = defaultRole.Id,
                CreatedAt = DateTime.UtcNow
            };
            
            _context.UserRoles.Add(userRole);
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();
            
            return Result.Success("Пользователь успешно зарегистрирован и авторизован.");
        }
        catch (DbUpdateException dbEx)
        {
            await transaction.RollbackAsync();
            return Result.Error($"Произошла ошибка при сохранении данных:{dbEx.Message}");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            
            return Result.Error($"Ошибка при регистрации пользователя: {ex.Message}");
        }
    }

    public async Task<string> GetIdByEmailAsync(string email)
    {
        return await _context.Users
            .Where(u => u.Email == email)
            .Select(u => u.Id)
            .FirstOrDefaultAsync();
    }
    
    public async Task<Result> UploadAvatarAsync(UploadAvatarDTO request)
    {
        try
        {
            if (request?.File == null || request.File.Length == 0) { return Result.Error("Файл не выбран"); }
            
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == request.UserId);
            if (user == null) { return Result.Error("Пользователь не найден"); }
            

            string imageUrl;
            await using (var memoryStream = new MemoryStream())
            {
                await request.File.CopyToAsync(memoryStream);
                memoryStream.Position = 0;

                var uploadedUrl = await _cloudinaryService.UploadAsync(memoryStream, request.File.FileName);
                if (uploadedUrl == null) { return Result.Error("Ошибка загрузки изображения в облако"); }
                
                imageUrl = uploadedUrl;
            }

            user.ImageProfileURL = imageUrl;
            user.LastAvatarUploadAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Result.Success(imageUrl);
        }
        catch (Exception ex)
        {
            return Result.Error($"Ошибка при загрузке аватара: {ex.Message}");
        }
    }

    public async Task<Result> UpdateUsernameAsync(UpdateUsernameRequestDTO request)
    {
        try
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == request.UserId);
            if (user == null)
            {
                return Result.Error("Пользователь не найден");
            }
            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == request.Username && u.Id != request.UserId);
            if (existingUser != null){ return Result.Error("Пользователь с таким именем уже существует"); }
            user.Username = request.Username;
            await _context.SaveChangesAsync();

            return Result.Success("Имя пользователя успешно обновлено");
        }
        catch (Exception ex)
        {
            return Result.Error($"Ошибка при обновлении имени пользователя: {ex.Message}");
        }
    }

    public async Task<UserProfileResponseDTO> GetProfileAsync(string userId)
    {
        var user = await _context.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == userId);
        
        if (user == null) { return null; }

        return new UserProfileResponseDTO
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            Name = user.Name,
            Surname = user.Surname,
            ImageProfileURL = user.ImageProfileURL,
            CreatedAt = user.CreatedAt,
            IsConfirmed = user.IsConfirmed,
            IsVerified = user.IsVerified,
            Rank = user.Rank,
            ReviewCount = user.ReviewCount,
            NegativeReviewCount = user.NegativeReviewCount,
            Balance = user.Balance,
            LastAvatarUploadAt = user.LastAvatarUploadAt,
            Roles = user.UserRoles?.Select(ur => ur.Role.Name).ToList() ?? new List<string>()
        };
    }
}