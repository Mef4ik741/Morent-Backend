using Microsoft.AspNetCore.Identity.UI.Services;

namespace WebAPI.Application.Services.Classes.AccountDirectoryServices;

public class DummyEmailSender : IEmailSender
{
    public Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        return Task.CompletedTask;
    }
}
