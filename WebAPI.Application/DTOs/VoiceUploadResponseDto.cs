namespace WebAPI.Application.DTOs;

public class VoiceUploadResponseDto
{
    public bool Success { get; set; }
    public string FileUrl { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
