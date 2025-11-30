using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using WebAPI.Application.DTOs;
using WebAPI.Application.DTOs.Response;

namespace WebAPI.Application.Services.Interfaces.AccountDirectoryIServices;

public interface IAccountService
{
    public Task VerifyEmailAsync(string id);
    public Task<Result> ConfirmEmailAsync(HttpContext context, ClaimsPrincipal User, string token);
    public Task<Result> RegisterAsync(RegisterRequestDTO request);
    public Task<string> GetIdByEmailAsync(string email);
    public Task<Result> UploadAvatarAsync(UploadAvatarDTO request);
    public Task<Result> UpdateUsernameAsync(UpdateUsernameRequestDTO request);
    public Task<UserProfileResponseDTO> GetProfileAsync(string userId);
}