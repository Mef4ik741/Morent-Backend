using System;
using System.Collections.Generic;

namespace WebAPI.Application.DTOs;

public class CreateUserRequest
{
    public string Name { get; set; }
    public string Surname { get; set; }
    public string Username { get; set; }
    public string Email { get; set; }
    public string Password { get; set; }
    public bool IsConfirmed { get; set; }
    public bool IsVerified { get; set; }
    public DateTime? CreatedAt { get; set; }
    public List<string> RoleIds { get; set; } = new();
}

