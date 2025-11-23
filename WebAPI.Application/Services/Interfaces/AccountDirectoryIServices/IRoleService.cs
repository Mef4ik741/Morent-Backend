using WebAPI.Application.DTOs;
using WebAPI.Application.DTOs.Response;

namespace WebAPI.Application.Services.Interfaces.AccountDirectoryIServices;

public interface IRoleService
{
    public Task<PaginatedResult<object>> GetAllRolesAsync(int page, int contentPerPage);
    public Task<TypedResult<object>> GetRoleByIdAsync(string id);
    public Task<Result> UpsertRoleAsync(UpsertRoleRequestDto request);
    public Task<Result> RemoveRoleAsync(RemoveRoleRequestDto request);
    public Task<Result> AssignRoleAsync(UserRoleRequestDto request);
    public Task<Result> UnAssignRoleAsync(UserRoleRequestDto request);
}
