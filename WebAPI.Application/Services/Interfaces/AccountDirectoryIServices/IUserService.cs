using Microsoft.AspNetCore.Mvc;
using WebAPI.Application.DTOs;
using UserSearchResultDto = WebAPI.Application.DTOs.UserSearchResultDto;

namespace WebAPI.Application.Services.Interfaces.AccountDirectoryIServices;

public interface IUserService
{ 
    Task<IActionResult> GetUsers(int page = 1, int pageSize = 10);
    Task<IActionResult> GetStats();
    Task<IActionResult> GetRoles();
    Task<IActionResult> SearchUsers(string query, int page = 1, int pageSize = 10);
    Task<IActionResult> CreateUser(CreateUserRequest request);
    Task<IActionResult> UpdateUser(string id, UpdateUserRequest request);
    Task<IActionResult> DeleteUser(string id);
    
    Task<IEnumerable<UserSearchResultDto>> SearchUsersByUsernameAsync(string query, int limit);
    Task<UserSearchResultDto?> GetUserByIdAsync(string userId);
    Task<IActionResult> GrantUserVerifiedRoleAsync(string userId);
}
