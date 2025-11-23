using WebAPI.Application.DTOs;
using WebAPI.Application.DTOs.Response;

namespace WebAPI.Application.Services.Interfaces.AccountDirectoryIServices;

public interface IAuthService
{
    public Task<TypedResult<object>> LoginAsync(LoginRequestDTO request);
    public Task<TypedResult<object>> LoginByUsernameAsync(LoginByUsernameRequestDTO request);
    public Task<TypedResult<object>> RefreshTokenAsync(string refreshToken);
    public Task<TypedResult<object>> RevokeTokenAsync(string refreshToken);
    public Task<TypedResult<object>> RevokeAllUserTokensAsync(string userId);
}