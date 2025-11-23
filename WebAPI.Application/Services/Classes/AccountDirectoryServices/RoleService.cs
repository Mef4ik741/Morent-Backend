using Microsoft.EntityFrameworkCore;
using WebAPI.Application.DTOs;
using WebAPI.Application.DTOs.Response;
using WebAPI.Application.Services.Interfaces.AccountDirectoryIServices;
using WebAPI.Domain.Models;
using WebAPI.Infrastructure.Data.Context;

namespace WebAPI.Application.Services.Classes.AccountDirectoryServices;

public class RoleService : IRoleService
{
    private readonly Context _context;

    public RoleService(Context context)
    {
        _context = context;
    }

    public async Task<PaginatedResult<object>> GetAllRolesAsync(int page, int contentPerPage)
    {
        var allResCount = await _context.Roles.CountAsync();

        var res = await _context.Roles
            .Skip((page - 1) * contentPerPage)
            .Take(contentPerPage)
            .Select(r => new { RoleId = r.Id, RoleName = r.Name })
            .ToListAsync();

        return PaginatedResult<object>.Success(res, allResCount);
    }

    public async Task<TypedResult<object>> GetRoleByIdAsync(string id)
    {
        Role role = await _context.Roles.FindAsync(id);

        return TypedResult<object>.Success(new
        {
            role.Id,
            role.Name
        });
    }

    public async Task<Result> UpsertRoleAsync(UpsertRoleRequestDto request)
    {
        string message; 
        var role = await _context.Roles.FindAsync(request.RoleId);

        if (request.RoleId != null && role != null)
        {
            role.Name = request.RoleName;
            message = "Role updated";
        }
        else
        {
            message = "Role added";
            await _context.Roles.AddAsync(new Role
            {
                Id = Guid.NewGuid().ToString(),
                Name = request.RoleName
            });
        }

        await _context.SaveChangesAsync();
        return Result.Success(message);
    }

    public async Task<Result> RemoveRoleAsync(RemoveRoleRequestDto request)
    {
        var role = await _context.Roles.FindAsync(request.RoleId);

        if (role == null) { throw new Exception("Role not found"); }
        _context.Roles.Remove(role);
        await _context.SaveChangesAsync();
        
        return Result.Success();
    }

    public async Task<Result> AssignRoleAsync(UserRoleRequestDto request)
    {
        var role = await _context.Roles.FindAsync(request.RoleId);
        if (role == null)
        {
            throw new Exception("Role not found");
        }
        
        var user = await _context.Users.FindAsync(request.UserId);
        if (user == null)
        {
            throw new Exception("User not found");
        }
        
        await _context.UserRoles.AddAsync(new()
        {
            RoleId = role.Id,
            UserId = user.Id
        });

        await _context.SaveChangesAsync();

        return Result.Success();
    }

    public async Task<Result> UnAssignRoleAsync(UserRoleRequestDto request)
    {
        var role = await _context.Roles.FindAsync(request.RoleId);
        if (role == null){ throw new Exception("Role not found"); }
        var user = await _context.Users.FindAsync(request.UserId);
        if (user == null){ throw new Exception("User not found"); }
        var userRole = await _context.UserRoles
            .FirstOrDefaultAsync(ur => ur.UserId == user.Id && ur.RoleId == role.Id);
        
        if (userRole == null) { throw new Exception("User does not hav this role"); }
        _context.UserRoles.Remove(userRole);

        await _context.SaveChangesAsync();
        return Result.Success();
    }
}