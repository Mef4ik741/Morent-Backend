using Microsoft.AspNetCore.Http;

namespace WebAPI.Application.DTOs;

public class UploadAvatarDTO
{
    public string UserId { get; set; }
    public IFormFile File { get; set; }
}

