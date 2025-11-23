using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.UI.Services;
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
    private readonly IWebHostEnvironment _env;
    private readonly IEmailSender _emailSender;
    private readonly ICloudinaryService _cloudinaryService;

    public AccountService(Context context, IWebHostEnvironment env, IEmailSender emailSender, ICloudinaryService cloudinaryService)
    {
        _context = context;
        _env = env;
        _emailSender = emailSender;
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

        var filePath = Path.Combine(_env.ContentRootPath, "wwwroot", "EmailConfirmation.html");
        if (!File.Exists(filePath))
            return Result.Error("Файл подтверждения не найден");

        var emailContent = await File.ReadAllTextAsync(filePath);
        var confirmationLink = $"{context.Request.Scheme}://{context.Request.Host}/api/Account/Email/Verify/{user.Id}/{token}";
        
        var message = emailContent
            .Replace("{Username}", username)
            .Replace("{ConfirmationLink}", confirmationLink);

        await _emailSender.SendEmailAsync(email, "Подтверждение email", message);
        
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

            var passwordPattern = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{8,}$";
            if (request.Password != request.ConfirmPassword)
                return Result.Error("Пароли не совпадают");
            if (!Regex.IsMatch(request.Password, passwordPattern))
                return Result.Error("Пароль должен быть не менее 8 символов, содержать строчные и прописные буквы и цифру");
            
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
                return Result.Error("Пользователь не найден");

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
        if (user == null){ return null; }

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

    public async Task<Result> InitiateLinkingAsync(InitiateLinkingDTO request)
    {
        try
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == request.Username);
            if (user == null){ return Result.Error("Пользователь не найден"); }
            
            return Result.Success($"Привязка инициирована для пользователя {request.Username}");
        }
        catch (Exception ex)
        {
            return Result.Error($"Ошибка при инициации привязки: {ex.Message}");
        }
    }

    public async Task<Result> SendLinkingCodeAsync(SendLinkingCodeDTO request)
    {
        try
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == request.Username);
            if (user == null)
                return Result.Error("Пользователь не найден");
            
            return Result.Success($"Код отправлен для Telegram ID: {request.TelegramId}");
        }
        catch (Exception ex)
        {
            return Result.Error($"Ошибка при отправке кода привязки: {ex.Message}");
        }
    }

    public async Task<Result> VerifyLinkingCodeAsync(VerifyLinkingCodeDTO request)
    {
        try
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == request.Username);
            if (user == null)
            {
                return Result.Error("Пользователь не найден");
            }
            using var httpClient = new HttpClient();
            var telegramBotRequest = new
            {
                request.Username,
                request.LinkingCode
            };

            var json = JsonSerializer.Serialize(telegramBotRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                var response = await httpClient.PostAsync("http://localhost:5260/api/telegram/verify-linking-code", content);
                
                if (response.IsSuccessStatusCode)
                {
                    return Result.Success("Код подтвержден, привязка завершена");
                }
                else
                {
                    return Result.Error("Неверный код привязки");
                }
            }
            catch (HttpRequestException)
            {
                return Result.Error("Сервис Telegram бота недоступен");
            }
        }
        catch (Exception ex)
        {
            return Result.Error($"Ошибка при проверке кода привязки: {ex.Message}");
        }
    }

    public async Task<Result> ForgotPasswordByUsernameAsync(ForgotPasswordByUsernameDTO request)
    {
        try
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == request.Username);
            if (user == null)
                return Result.Error("Пользователь с таким никнеймом не найден");

            var resetToken = Guid.NewGuid().ToString("N")[..8];
            
            return Result.Success($"Токен восстановления сгенерирован: {resetToken}");
        }
        catch (Exception ex)
        {
            return Result.Error($"Ошибка при генерации токена восстановления: {ex.Message}");
        }
    }

    public async Task<Result> VerifyResetTokenByUsernameAsync(VerifyResetTokenByUsernameDTO request)
    {
        try
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == request.Username);
            if (user == null)
                return Result.Error("Пользователь не найден");

            if (string.IsNullOrEmpty(request.Token) || request.Token.Length != 8)
                return Result.Error("Неверный формат токена");

            return Result.Success("Токен подтвержден");
        }
        catch (Exception ex)
        {
            return Result.Error($"Ошибка при проверке токена: {ex.Message}");
        }
    }

    public async Task<Result> ResetPasswordByUsernameAsync(ResetPasswordByUsernameDTO request)
    {
        try
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == request.Username);
            
            if (user == null)
            {
                return Result.Error("Пользователь не найден");
            }
            
            if (string.IsNullOrEmpty(request.Token) || request.Token.Length != 8)
            {
                return Result.Error("Неверный токен");
            }

            if (string.IsNullOrEmpty(request.NewPassword) || request.NewPassword.Length < 6)
            {
                return Result.Error("Пароль должен содержать минимум 6 символов"); 
            }
            
            user.Password = HashPassword(request.NewPassword);
            await _context.SaveChangesAsync();
            
            return Result.Success("Пароль успешно изменен");
        }
        catch (Exception ex)
        {
            return Result.Error($"Ошибка при сбросе пароля: {ex.Message}");
        }
    }
}