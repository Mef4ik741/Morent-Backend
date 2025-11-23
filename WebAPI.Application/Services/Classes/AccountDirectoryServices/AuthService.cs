using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using WebAPI.Application.DTOs;
using WebAPI.Application.DTOs.Response;
using WebAPI.Application.Services.Interfaces.AccountDirectoryIServices;
using WebAPI.Infrastructure.Data.Context;
using static BCrypt.Net.BCrypt;

namespace WebAPI.Application.Services.Classes.AccountDirectoryServices;

public class AuthService : IAuthService
{
    private readonly Context _context;
    private readonly ITokenService _tokenService;

    public AuthService(Context context, ITokenService tokenService)
    {
        _context = context;
        _tokenService = tokenService;
    }

    public async Task<TypedResult<object>> LoginAsync(LoginRequestDTO request)
    {
        var user = _context.Users.FirstOrDefault(u => u.Email == request.Email);

        if (user == null || !Verify(request.Password, user.Password))
        {
            return TypedResult<object>.Error("Invalid login credentials");
        }

        var userRoles = await _context.UserRoles.Include(ur => ur.Role)
            .Where(ur => ur.UserId == user.Id)
            .Select(r => r.Role.Name).ToListAsync();
        
        var accessToken = await _tokenService.CreateAccessTokenAsync(user, userRoles);
        var refreshToken = await _tokenService.CreateRefreshTokenAsync(user);

        return TypedResult<object>.Success(new
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken.Token,
            User = new {
                user.Id,
                user.Email,
                user.Username,
                user.ImageProfileURL
            }
        });
    }

    public async Task<TypedResult<object>> LoginByUsernameAsync(LoginByUsernameRequestDTO request)
    {
        var user = _context.Users.FirstOrDefault(u => u.Username == request.Username);

        if (user == null || !Verify(request.Password, user.Password))
        {
            return TypedResult<object>.Error("Invalid login credentials");
        }

        var userRoles = await _context.UserRoles.Include(ur => ur.Role)
            .Where(ur => ur.UserId == user.Id)
            .Select(r => r.Role.Name).ToListAsync();
        
        var accessToken = await _tokenService.CreateAccessTokenAsync(user, userRoles);
        var refreshToken = await _tokenService.CreateRefreshTokenAsync(user);

        return TypedResult<object>.Success(new
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken.Token,
            User = new {
                user.Id,
                user.Email,
                user.Username,
                user.ImageProfileURL
            }
        });
    }

    public async Task<TypedResult<object>> RefreshTokenAsync(string refreshToken)
    {
        try
        {
            var newAccessToken = await _tokenService.RefreshAccessTokenAsync(refreshToken);
            
            return TypedResult<object>.Success(new
            {
                AccessToken = newAccessToken,
                Message = "Token successfully refreshed"
            });
        }
        catch (SecurityTokenException ex)
        {
            return TypedResult<object>.Error(ex.Message);
        }
    }

    public async Task<TypedResult<object>> RevokeTokenAsync(string refreshToken)
    {
        var result = await _tokenService.RevokeRefreshTokenAsync(refreshToken);
        
        if (result)
        {
            return TypedResult<object>.Success(new { Message = "Token successfully revoked" });
        }
        
        return TypedResult<object>.Error("Token not found or already revoked");
    }

    public async Task<TypedResult<object>> RevokeAllUserTokensAsync(string userId)
    {
        var result = await _tokenService.RevokeAllUserRefreshTokensAsync(userId);
        
        if (result)
        {
            return TypedResult<object>.Success(new { Message = "All user tokens successfully revoked" });
        }
        
        return TypedResult<object>.Error("No active tokens found for user");
    }
}