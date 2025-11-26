using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using WebAPI.Application.Services.Interfaces.AccountDirectoryIServices;
using RestSharp;

namespace WebAPI.Application.Services.Classes.AccountDirectoryServices;

public class EmailService : IEmailService, IDisposable
{
    private readonly IConfiguration _configuration;
    private readonly RestClient _client;
    private readonly string _fromEmail;
    private readonly string _fromName;

    public EmailService(IConfiguration configuration)
    {
        _configuration = configuration;
        _fromEmail = _configuration["Email:From"] ?? _configuration["Email:Username"]!;
        _fromName = _configuration["Email:FromName"] ?? "Morent";

        var apiKey = _configuration["Email:ApiKey"];
        var secretKey = _configuration["Email:SecretKey"];

        _client = new RestClient("https://api.mailjet.com/v3.1/send");
        _client.AddDefaultHeader("Authorization", "Basic " + 
            Convert.ToBase64String(Encoding.UTF8.GetBytes($"{apiKey}:{secretKey}")));
        _client.AddDefaultHeader("Content-Type", "application/json");
    }

    private async Task SendMailjetEmailAsync(string toEmail, string toName, string subject, string htmlContent)
    {
        var body = new
        {
            Messages = new[]
            {
                new {
                    From = new { Email = _fromEmail, Name = _fromName },
                    To = new[] { new { Email = toEmail, Name = toName } },
                    Subject = subject,
                    HTMLPart = htmlContent
                }
            }
        };

        var request = new RestRequest("", Method.Post).AddJsonBody(body);
        var response = await _client.ExecuteAsync(request);

        if (!response.IsSuccessful)
        {
            throw new Exception($"Mailjet API error: {response.Content}");
        }
    }

    public async Task SendEmailAsync(string email, string subject, string content)
    {
        await SendMailjetEmailAsync(email, email, subject, content);
    }

    public async Task SendEmailConfirmationAsync(string email, string username, string confirmationLink)
    {
        var subject = "✅ Подтверждение регистрации — Morent";
        
        string htmlTemplate;
        var templatePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "EmailConfirmation.html");
        if (File.Exists(templatePath))
        {
            htmlTemplate = await File.ReadAllTextAsync(templatePath);
        }
        else
        {
            htmlTemplate = $@"
            <div style='font-family:Segoe UI, Arial; text-align:center;'>
                <h2>Здравствуйте, {username}!</h2>
                <p>Подтвердите email: <a href='{confirmationLink}'>Нажмите сюда</a></p>
            </div>";
        }

        var content = htmlTemplate.Replace("{Username}", username).Replace("{ConfirmationLink}", confirmationLink);

        await SendMailjetEmailAsync(email, username, subject, content);
    }

    public async Task SendPasswordResetEmailAsync(string email, string username, string resetLink)
    {
        var subject = "🔑 Сброс пароля — Morent";
        var content = $@"
        <h2>Здравствуйте, {username}!</h2>
        <p>Сброс пароля: <a href='{resetLink}'>Нажмите сюда</a></p>";
        
        await SendMailjetEmailAsync(email, username, subject, content);
    }

    public async Task SendWelcomeEmailAsync(string email, string username)
    {
        var subject = "🎉 Добро пожаловать в Morent!";
        var content = $@"
        <h2>Добро пожаловать, {username}!</h2>
        <p>Ваш аккаунт подтвержден. Наслаждайтесь сервисом Morent.</p>";
        
        await SendMailjetEmailAsync(email, username, subject, content);
    }

    public void Dispose()
    {
        _client?.Dispose();
    }
}
