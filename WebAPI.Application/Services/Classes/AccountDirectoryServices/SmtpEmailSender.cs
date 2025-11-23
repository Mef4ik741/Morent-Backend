using System.Net;
using System.Net.Mail;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Configuration;

namespace WebAPI.Application.Services.Classes.AccountDirectoryServices;

public class SmtpEmailSender : IEmailSender
{
    private readonly IConfiguration _configuration;

    public SmtpEmailSender(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        var host = _configuration["Email:Host"];
        var port = _configuration.GetValue<int?>("Email:Port") ?? 587;
        var enableSsl = _configuration.GetValue<bool?>("Email:EnableSsl") ?? true;
        var userName = _configuration["Email:Username"];
        var password = _configuration["Email:Password"];
        var from = _configuration["Email:From"] ?? userName;

        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(from))
        {
            // SMTP не настроен — тихо выходим, чтобы не падало приложение
            return;
        }

        using var client = new SmtpClient(host, port)
        {
            EnableSsl = enableSsl,
            Credentials = new NetworkCredential(userName, password)
        };

        using var message = new MailMessage
        {
            From = new MailAddress(from),
            Subject = subject,
            Body = htmlMessage,
            IsBodyHtml = true
        };

        message.To.Add(email);

        await client.SendMailAsync(message);
    }
}
