using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using WebAPI.Application.Services.Interfaces.AccountDirectoryIServices;

namespace WebAPI.Application.Services.Classes.AccountDirectoryServices;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly SmtpClient _client;
    
    public EmailService(IConfiguration configuration)
    {
        _configuration = configuration;
        _client = new SmtpClient
        {
            Host = _configuration["Email:Host"] ?? "smtp.gmail.com",
            Port = int.Parse(_configuration["Email:Port"] ?? "587"),
            EnableSsl = true,
            Credentials = new NetworkCredential(
                _configuration["Email:Username"],
                _configuration["Email:Password"]
            )
        };
    }

    public async Task SendEmailAsync(string email, string subject, string content)
    {
        try
        {
            var message = new MailMessage
            {
                From = new MailAddress(_configuration["Email:From"] ?? _configuration["Email:Username"]!),
                Subject = subject,
                Body = content,
                IsBodyHtml = true
            };

            message.To.Add(new MailAddress(email));

            await _client.SendMailAsync(message);
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task SendEmailConfirmationAsync(string email, string username, string confirmationLink)
    {
        var subject = "✅ Подтверждение регистрации — Morent";
        
        try
        {
            var templatePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "EmailConfirmation.html");
            var htmlTemplate = await File.ReadAllTextAsync(templatePath);
            
            var content = htmlTemplate
                .Replace("{Username}", username)
                .Replace("{ConfirmationLink}", confirmationLink);

            await SendEmailAsync(email, subject, content);
        }
        catch (FileNotFoundException)
        {
            var content = $@"
            <!DOCTYPE html>
            <html lang='ru'>
            <head>
                <meta charset='UTF-8'>
                <title>Подтверждение Email — Morent</title>
                <style>
                    body {{ margin: 0; padding: 0; background: #f2f4f8; font-family: 'Segoe UI', Arial, sans-serif; }}
                    .container {{ max-width: 600px; margin: 40px auto; background: #ffffff; border-radius: 12px; box-shadow: 0 4px 20px rgba(0,0,0,0.05); overflow: hidden; }}
                    .header {{ background: linear-gradient(135deg, #4f93ff, #0069d9); padding: 30px; text-align: center; color: #ffffff; }}
                    .header h1 {{ margin: 0; font-size: 28px; font-weight: 600; }}
                    .content {{ padding: 40px 30px; text-align: center; }}
                    .content h2 {{ color: #333333; font-size: 24px; margin-bottom: 20px; }}
                    .content p {{ color: #555555; font-size: 16px; line-height: 1.6; margin-bottom: 30px; }}
                    .button {{ display: inline-block; padding: 14px 28px; background: #007bff; color: #ffffff !important; text-decoration: none; border-radius: 8px; font-size: 16px; font-weight: 600; }}
                    .footer {{ padding: 20px 30px; text-align: center; color: #999999; font-size: 13px; background: #f9f9f9; }}
                    .footer strong {{ color: #4f93ff; }}
                </style>
            </head>
            <body>
            <div class='container'>
                <div class='header'>
                    <h1>Morent</h1>
                </div>
                <div class='content'>
                    <h2>Здравствуйте, {username}!</h2>
                    <p>Благодарим за регистрацию в <strong>Morent</strong>. Чтобы завершить процесс, подтвердите свой адрес электронной почты, нажав на кнопку ниже:</p>
                    <a class='button' href='{confirmationLink}'>Подтвердить Email</a>
                    <p style='margin-top: 25px; font-size: 14px; color: #888;'>Если вы не запрашивали регистрацию, просто проигнорируйте это письмо.</p>
                </div>
                <div class='footer'>
                    &copy; 2025 <strong>Morent</strong>. Все права защищены.
                </div>
            </div>
            </body>
            </html>";

            await SendEmailAsync(email, subject, content);
        }
    }

    public async Task SendPasswordResetEmailAsync(string email, string username, string resetLink)
    {
        var subject = "🔑 Сброс пароля — Morent";
        
        var content = $@"
        <!DOCTYPE html>
        <html lang='ru'>
        <head>
            <meta charset='UTF-8'>
            <title>Сброс пароля — Morent</title>
            <style>
                body {{ margin: 0; padding: 0; background: #f2f4f8; font-family: 'Segoe UI', Arial, sans-serif; }}
                .container {{ max-width: 600px; margin: 40px auto; background: #ffffff; border-radius: 12px; box-shadow: 0 4px 20px rgba(0,0,0,0.05); overflow: hidden; }}
                .header {{ background: linear-gradient(135deg, #ff6b6b, #ee5a52); padding: 30px; text-align: center; color: #ffffff; }}
                .header h1 {{ margin: 0; font-size: 28px; font-weight: 600; }}
                .content {{ padding: 40px 30px; text-align: center; }}
                .content h2 {{ color: #333333; font-size: 24px; margin-bottom: 20px; }}
                .content p {{ color: #555555; font-size: 16px; line-height: 1.6; margin-bottom: 30px; }}
                .button {{ display: inline-block; padding: 14px 28px; background: #ff6b6b; color: #ffffff !important; text-decoration: none; border-radius: 8px; font-size: 16px; font-weight: 600; }}
                .footer {{ padding: 20px 30px; text-align: center; color: #999999; font-size: 13px; background: #f9f9f9; }}
                .footer strong {{ color: #ff6b6b; }}
            </style>
        </head>
        <body>
        <div class='container'>
            <div class='header'>
                <h1>Morent</h1>
            </div>
            <div class='content'>
                <h2>Здравствуйте, {username}!</h2>
                <p>Мы получили запрос на сброс пароля для вашего аккаунта в <strong>Morent</strong>. Нажмите на кнопку ниже, чтобы создать новый пароль:</p>
                <a class='button' href='{resetLink}'>Сбросить пароль</a>
                <p style='margin-top: 25px; font-size: 14px; color: #888;'>Если вы не запрашивали сброс пароля, просто проигнорируйте это письмо. Ссылка действительна в течение 1 часа.</p>
            </div>
            <div class='footer'>
                &copy; 2025 <strong>Morent</strong>. Все права защищены.
            </div>
        </div>
        </body>
        </html>";

        await SendEmailAsync(email, subject, content);
    }

    public async Task SendWelcomeEmailAsync(string email, string username)
    {
        var subject = "🎉 Добро пожаловать в Morent!";
        
        var content = $@"
        <!DOCTYPE html>
        <html lang='ru'>
        <head>
            <meta charset='UTF-8'>
            <title>Добро пожаловать в Morent</title>
            <style>
                body {{ margin: 0; padding: 0; background: #f2f4f8; font-family: 'Segoe UI', Arial, sans-serif; }}
                .container {{ max-width: 600px; margin: 40px auto; background: #ffffff; border-radius: 12px; box-shadow: 0 4px 20px rgba(0,0,0,0.05); overflow: hidden; }}
                .header {{ background: linear-gradient(135deg, #28a745, #20c997); padding: 30px; text-align: center; color: #ffffff; }}
                .header h1 {{ margin: 0; font-size: 28px; font-weight: 600; }}
                .content {{ padding: 40px 30px; text-align: center; }}
                .content h2 {{ color: #333333; font-size: 24px; margin-bottom: 20px; }}
                .content p {{ color: #555555; font-size: 16px; line-height: 1.6; margin-bottom: 20px; }}
                .features {{ background: #f8f9fa; padding: 20px; border-radius: 8px; margin: 20px 0; text-align: left; }}
                .features h3 {{ color: #28a745; margin-bottom: 15px; }}
                .features ul {{ list-style: none; padding: 0; }}
                .features li {{ padding: 5px 0; color: #555; }}
                .features li:before {{ content: '✅ '; margin-right: 8px; }}
                .button {{ display: inline-block; padding: 14px 28px; background: #28a745; color: #ffffff !important; text-decoration: none; border-radius: 8px; font-size: 16px; font-weight: 600; }}
                .footer {{ padding: 20px 30px; text-align: center; color: #999999; font-size: 13px; background: #f9f9f9; }}
                .footer strong {{ color: #28a745; }}
            </style>
        </head>
        <body>
        <div class='container'>
            <div class='header'>
                <h1>Morent</h1>
            </div>
            <div class='content'>
                <h2>Добро пожаловать, {username}! 🎉</h2>
                <p>Поздравляем! Ваш аккаунт в <strong>Morent</strong> успешно подтвержден. Теперь вы можете пользоваться всеми возможностями нашего сервиса аренды автомобилей.</p>
                
                <div class='features'>
                    <h3>Что вы можете делать:</h3>
                    <ul>
                        <li>Арендовать автомобили</li>
                        <li>Добавлять свои автомобили для аренды</li>
                        <li>Управлять бронированиями</li>
                        <li>Получать уведомления в реальном времени</li>
                        <li>Добавлять автомобили в избранное</li>
                    </ul>
                </div>
                
                <a class='button' href='http://localhost:5173'>Начать использовать Morent</a>
                <p style='margin-top: 25px; font-size: 14px; color: #888;'>Если у вас есть вопросы, свяжитесь с нашей службой поддержки.</p>
            </div>
            <div class='footer'>
                &copy; 2025 <strong>Morent</strong>. Все права защищены.
            </div>
        </div>
        </body>
        </html>";

        await SendEmailAsync(email, subject, content);
    }

    public void Dispose()
    {
        _client?.Dispose();
    }
}
