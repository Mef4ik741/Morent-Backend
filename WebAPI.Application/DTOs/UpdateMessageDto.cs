namespace WebAPI.Application.DTOs;

public class UpdateMessageDto
{
    public string UserId { get; set; } = string.Empty; // кто редактирует (должен быть отправителем)
    public string Message { get; set; } = string.Empty; // новый текст
}
