namespace WebAPI.Application.Services.Interfaces.AccountDirectoryIServices;

public interface IEmailService
{
    Task SendEmailAsync(string email, string subject, string content);
    Task SendEmailConfirmationAsync(string email, string username, string confirmationLink);
    Task SendPasswordResetEmailAsync(string email, string username, string resetLink);
    Task SendWelcomeEmailAsync(string email, string username);
}
