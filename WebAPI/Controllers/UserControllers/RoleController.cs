using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Application.DTOs;
using WebAPI.Application.Services.Interfaces.AccountDirectoryIServices;

namespace WebAPI.Controllers.UserControllers;

[Authorize(Policy = "AdminPolicy")]
[ApiController]
[Route("api/[controller]")]
public class RoleController : ControllerBase
{
    private readonly IRoleService _roleService;

    public RoleController(IRoleService roleService)
    {
        _roleService = roleService;
    }

    [HttpGet("All/{page}/{pageSize}")]
    public async Task<IActionResult> GetAllRolesAsync(int page=1, int pageSize=15)
    {
        return Ok(await _roleService.GetAllRolesAsync(page, pageSize));
    }
    
    [HttpPost("New")]
    public async Task<IActionResult> AddNewRoleAsync(UpsertRoleRequestDto request)
    {
        return Ok(await _roleService.UpsertRoleAsync(request));
    }
    
    [HttpDelete("Remove")]
    public async Task<IActionResult> RemoveRoleAsync(RemoveRoleRequestDto request)
    {
        return Ok(await _roleService.RemoveRoleAsync(request));
    }
    
    [HttpGet("{id}")]
    public async Task<IActionResult> GetRoleAsync(string id)
    {
        return Ok(await _roleService.GetRoleByIdAsync(id));
    }
    
    [HttpPost("Assign")]
    public async Task<IActionResult> AssignRoleAsync(UserRoleRequestDto request)
    {
        return Ok(await _roleService.AssignRoleAsync(request));
    }

    [HttpGet("UnAssign")]
    public async Task<IActionResult> UnAssignRoleAsync(UserRoleRequestDto request)
    {
        return Ok(await _roleService.UnAssignRoleAsync(request));
    }
}