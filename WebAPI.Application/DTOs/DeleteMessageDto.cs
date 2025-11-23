namespace WebAPI.Application.DTOs;

public class DeleteMessageDto
{
    public string UserId { get; set; } = string.Empty; // кто удаляет (должен быть отправителем)
}
