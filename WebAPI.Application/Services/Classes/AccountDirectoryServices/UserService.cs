using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebAPI.Application.DTOs;
using WebAPI.Application.Services.Interfaces.AccountDirectoryIServices;
using WebAPI.Domain.Models;
using WebAPI.Infrastructure.Data.Context;
using UserSearchResultDto = WebAPI.Application.DTOs.UserSearchResultDto;

namespace WebAPI.Application.Services.Classes.AccountDirectoryServices;

public class UserService : IUserService
{
    private readonly Context _context;

    public UserService(Context context)
    {
        _context = context;
    }

    public async Task<IActionResult> GetUsers()
    {
        try
        {
            var users = await _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .Select(u => new
                {
                    u.Id,
                    u.Name,
                    u.Surname,
                    u.Username,
                    u.Email,
                    u.IsConfirmed,
                    u.IsVerified,
                    u.Rank,
                    u.ReviewCount,
                    u.NegativeReviewCount,
                    u.CreatedAt,
                    Roles = u.UserRoles.Select(ur => ur.Role.Name).ToList()
                })
                .ToListAsync();

            return new OkObjectResult(users);
        }
        catch (Exception ex)
        {
            return new ObjectResult(new { error = ex.Message }) { StatusCode = 500 };
        }
    }

    public async Task<IActionResult> SearchUsers(string query)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return new BadRequestObjectResult(new { message = "Параметр query обязателен" });
            }

            var q = query.Trim().ToLower();

            var users = await _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .Where(u => u.Username.ToLower().Contains(q) || u.Email.ToLower().Contains(q))
                .Select(u => new
                {
                    u.Id,
                    u.Name,
                    u.Surname,
                    u.Username,
                    u.Email,
                    u.IsConfirmed,
                    u.IsVerified,
                    u.Rank,
                    u.ReviewCount,
                    u.NegativeReviewCount,
                    u.CreatedAt,
                    Roles = u.UserRoles.Select(ur => ur.Role.Name).ToList()
                })
                .ToListAsync();

            return new OkObjectResult(users);
        }
        catch (Exception ex)
        {
            return new ObjectResult(new { error = ex.Message }) { StatusCode = 500 };
        }
    }

    public async Task<IActionResult> CreateUser(CreateUserRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            {
                return new BadRequestObjectResult(new { message = "Username, Email и Password обязательны" });
            }

            var usernameTaken = await _context.Users.AnyAsync(u => u.Username == request.Username);
            if (usernameTaken)
                return new BadRequestObjectResult(new { message = "Username уже занят" });

            var emailTaken = await _context.Users.AnyAsync(u => u.Email == request.Email);
            if (emailTaken)
                return new BadRequestObjectResult(new { message = "Email уже занят" });

            var entity = new User
            {
                Id = Guid.NewGuid().ToString(),
                Name = request.Name,
                Surname = request.Surname,
                Username = request.Username,
                Email = request.Email,
                Password = BCrypt.Net.BCrypt.HashPassword(request.Password),
                IsConfirmed = request.IsConfirmed,
                IsVerified = request.IsVerified,
                CreatedAt = request.CreatedAt ?? DateTime.UtcNow
            };

            _context.Users.Add(entity);
            await _context.SaveChangesAsync();

            if (request.RoleIds != null && request.RoleIds.Count > 0)
            {
                var roles = await _context.Roles
                    .Where(r => request.RoleIds.Contains(r.Id))
                    .Select(r => r.Id)
                    .ToListAsync();

                if (roles.Count == 0)
                {
                    return new BadRequestObjectResult(new { message = "Указанные роли не найдены" });
                }

                var links = roles.Select(roleId => new UserRole
                {
                    UserId = entity.Id,
                    RoleId = roleId
                }).ToList();

                _context.UserRoles.AddRange(links);
                await _context.SaveChangesAsync();
            }

            return new OkObjectResult(new { message = "Пользователь создан", id = entity.Id });
        }
        catch (Exception ex)
        {
            return new ObjectResult(new { error = ex.Message }) { StatusCode = 500 };
        }
    }

    public async Task<IActionResult> UpdateUser(string id, UpdateUserRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(id))
                return new BadRequestObjectResult(new { message = "Id обязателен" });

            var existing = await _context.Users.Include(u => u.UserRoles).FirstOrDefaultAsync(u => u.Id == id);
            if (existing == null)
                return new NotFoundObjectResult(new { message = "Пользователь не найден" });

            if (!string.IsNullOrWhiteSpace(request.Username) && request.Username != existing.Username)
            {
                var usernameTaken = await _context.Users.AnyAsync(u => u.Username == request.Username && u.Id != id);
                if (usernameTaken)
                    return new BadRequestObjectResult(new { message = "Username уже занят" });
            }

            if (!string.IsNullOrWhiteSpace(request.Email) && request.Email != existing.Email)
            {
                var emailTaken = await _context.Users.AnyAsync(u => u.Email == request.Email && u.Id != id);
                if (emailTaken)
                    return new BadRequestObjectResult(new { message = "Email уже занят" });
            }

            if (request.Name != null) existing.Name = request.Name;
            if (request.Surname != null) existing.Surname = request.Surname;
            if (request.Username != null) existing.Username = request.Username;
            if (request.Email != null) existing.Email = request.Email;
            if (!string.IsNullOrWhiteSpace(request.Password))
            {
                existing.Password = BCrypt.Net.BCrypt.HashPassword(request.Password);
            }
            if (request.IsConfirmed.HasValue) existing.IsConfirmed = request.IsConfirmed.Value;
            if (request.IsVerified.HasValue) existing.IsVerified = request.IsVerified.Value;
            if (request.CreatedAt.HasValue) existing.CreatedAt = request.CreatedAt.Value;

            await _context.SaveChangesAsync();

            if (request.RoleIds != null)
            {
                var currentRoleIds = existing.UserRoles?.Select(ur => ur.RoleId).ToHashSet() ?? new HashSet<string>();
                var desiredRoleIds = request.RoleIds.Where(id => !string.IsNullOrWhiteSpace(id)).ToHashSet();

                var validRoleIds = await _context.Roles
                    .Where(r => desiredRoleIds.Contains(r.Id))
                    .Select(r => r.Id)
                    .ToListAsync();

                var toAdd = validRoleIds.Where(rid => !currentRoleIds.Contains(rid)).ToList();
                var toRemove = currentRoleIds.Where(rid => !desiredRoleIds.Contains(rid)).ToList();

                if (toRemove.Count > 0 && existing.UserRoles != null && existing.UserRoles.Count > 0)
                {
                    var removeLinks = existing.UserRoles.Where(ur => toRemove.Contains(ur.RoleId)).ToList();
                    _context.UserRoles.RemoveRange(removeLinks);
                }

                if (toAdd.Count > 0)
                {
                    var addLinks = toAdd.Select(rid => new UserRole { UserId = existing.Id, RoleId = rid });
                    await _context.UserRoles.AddRangeAsync(addLinks);
                }

                await _context.SaveChangesAsync();
            }

            return new OkObjectResult(new { message = "Пользователь обновлён" });
        }
        catch (Exception ex)
        {
            return new ObjectResult(new { error = ex.Message }) { StatusCode = 500 };
        }
    }

    public async Task<IActionResult> DeleteUser(string id)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(id))
                return new BadRequestObjectResult(new { message = "Id обязателен" });

            var user = await _context.Users.Include(u => u.UserRoles).FirstOrDefaultAsync(u => u.Id == id);
            if (user == null)
                return new NotFoundObjectResult(new { message = "Пользователь не найден" });

            if (user.UserRoles != null && user.UserRoles.Count > 0)
            {
                _context.UserRoles.RemoveRange(user.UserRoles);
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return new OkObjectResult(new { message = "Пользователь удалён" });
        }
        catch (Exception ex)
        {
            return new ObjectResult(new { error = ex.Message }) { StatusCode = 500 };
        }
    }

    public async Task<IActionResult> GetStats()
    {
        try
        {
            var totalUsers = await _context.Users.CountAsync();
            var todayUsers = await _context.Users
                .Where(u => u.CreatedAt.Date == DateTime.UtcNow.Date)
                .CountAsync();
            var weekUsers = await _context.Users
                .Where(u => u.CreatedAt >= DateTime.UtcNow.AddDays(-7))
                .CountAsync();
            var monthUsers = await _context.Users
                .Where(u => u.CreatedAt >= DateTime.UtcNow.AddDays(-30))
                .CountAsync();
            var verifiedUsers = await _context.Users
                .Where(u => u.IsVerified)
                .CountAsync();

            var stats = new
            {
                TotalUsers = totalUsers,
                TodayUsers = todayUsers,
                WeekUsers = weekUsers,
                MonthUsers = monthUsers,
                VerifiedUsers = verifiedUsers
            };

            return new OkObjectResult(stats);
        }
        catch (Exception ex)
        {
            return new ObjectResult(new { error = ex.Message }) { StatusCode = 500 };
        }
    }

    public async Task<IActionResult> GetRoles()
    {
        try
        {
            var roles = await _context.Roles
                .Select(r => new
                {
                    r.Id,
                    r.Name,
                    r.CreatedAt
                })
                .ToListAsync();

            return new OkObjectResult(roles);
        }
        catch (Exception ex)
        {
            return new ObjectResult(new { error = ex.Message }) { StatusCode = 500 };
        }
    }

    // Новые методы для поиска пользователей для чата
    public async Task<IEnumerable<UserSearchResultDto>> SearchUsersByUsernameAsync(string query, int limit)
    {
        try
        {
            var users = await _context.Users
                .Where(u => u.Username.ToLower().Contains(query.ToLower()) || 
                           u.Name.ToLower().Contains(query.ToLower()) ||
                           u.Surname.ToLower().Contains(query.ToLower()))
                .Take(limit)
                .Select(u => new UserSearchResultDto(
                    u.Id,
                    u.Username,
                    u.Name,
                    u.Surname,
                    u.ImageProfileURL,
                    u.IsVerified,
                    u.CreatedAt,
                    false
                ))
                .ToListAsync();

            return users;
        }
        catch (Exception)
        {
            return new List<UserSearchResultDto>();
        }
    }

    public async Task<UserSearchResultDto?> GetUserByIdAsync(string userId)
    {
        try
        {
            var user = await _context.Users
                .Where(u => u.Id == userId)
                .Select(u => new UserSearchResultDto(
                    u.Id,
                    u.Username,
                    u.Name,
                    u.Surname,
                    u.ImageProfileURL,
                    u.IsVerified,
                    u.CreatedAt,
                    false
                ))
                .FirstOrDefaultAsync();

            return user;
        }
        catch (Exception)
        {
            return null;
        }
    }

    public async Task<IActionResult> GrantUserVerifiedRoleAsync(string userId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return new BadRequestObjectResult(new { message = "UserId is required" });
            }

            var user = await _context.Users
                .Include(u => u.UserRoles)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return new NotFoundObjectResult(new { message = "User not found" });
            }

            var role = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "UserVerified");
            if (role == null)
            {
                return new NotFoundObjectResult(new { message = "Role 'UserVerified' not found" });
            }

            var hasRole = await _context.UserRoles
                .AnyAsync(ur => ur.UserId == user.Id && ur.RoleId == role.Id);

            if (!hasRole)
            {
                var userRole = new UserRole
                {
                    UserId = user.Id,
                    RoleId = role.Id
                };

                await _context.UserRoles.AddAsync(userRole);
            }

            if (!user.IsVerified)
            {
                user.IsVerified = true;
            }

            await _context.SaveChangesAsync();

            return new OkObjectResult(new { message = "User granted UserVerified role" });
        }
        catch (Exception ex)
        {
            return new ObjectResult(new { error = ex.Message }) { StatusCode = 500 };
        }
    }
}