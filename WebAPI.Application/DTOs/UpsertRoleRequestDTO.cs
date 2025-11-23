namespace WebAPI.Application.DTOs;

public record UpsertRoleRequestDto(string RoleName, string? RoleId=null);
