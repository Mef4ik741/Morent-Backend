using System.Security.Claims;
using WebAPI.Domain.Models;

namespace WebAPI.Application.Services.Interfaces.AccountDirectoryIServices;

public interface ITokenService
{
    public Task<string> CreateAccessTokenAsync(User user, List<string> userRoles);
    public Task<string> CreateEmailConfirmationTokenAsync(ClaimsPrincipal user);
    public Task<RefreshToken> CreateRefreshTokenAsync(User user);
    public Task<string> RefreshAccessTokenAsync(string refreshToken);
    public Task<bool> RevokeRefreshTokenAsync(string refreshToken, string reason = "Revoked by user");
    public Task<bool> RevokeAllUserRefreshTokensAsync(string userId, string reason = "Revoked all tokens");
    public Task<string> GetEmailFromToken(string token);
    public Task<bool> ValidateEmailConfirmationTokenAsync(string token);
}