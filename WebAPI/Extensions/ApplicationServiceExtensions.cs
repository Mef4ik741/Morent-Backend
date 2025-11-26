using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using WebAPI.Application.Cloudinary;
using WebAPI.Infrastructure.Data.Context;
using WebAPI.Application.Services.Interfaces.BusinessLogicIServices;
using WebAPI.Application.Services.Classes.AccountDirectoryServices;
using WebAPI.Application.Services.Classes.BusinessLogicServices;
using WebAPI.Application.Services.Interfaces.AccountDirectoryIServices;

namespace WebAPI.Extensions;

public static class ApplicationServiceExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Контроллеры
        services.AddControllers();

        // Swagger
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Morent API",
                Version = "v1",
                Description = "Authentication and Business API"
            });

            // Use full type names for schema Ids to avoid collisions
            c.CustomSchemaIds(type => type.FullName);

            // JWT Bearer security definition
            var securityScheme = new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Description = "Введите только JWT токен, без префикса 'Bearer'",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            };

            c.AddSecurityDefinition("Bearer", securityScheme);
            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                { securityScheme, Array.Empty<string>() }
            });
        });

        // БД
        services.AddDbContext<Context>(options =>
            options.UseNpgsql(configuration.GetConnectionString("Default")));

        // Сервисы
        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IRoleService, RoleService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IReviewService, ReviewService>();
        
        services.AddScoped<IBookingsService, BookingsService>();
        services.AddScoped<IRentNotificationService, RentNotificationService>();
        services.AddScoped<ICarsService, CarsService>();
        services.AddScoped<IFavoritesService, FavoritesService>();

        // SignalR с JWT аутентификацией
        services.AddSignalR(options =>
        {
            options.EnableDetailedErrors = true;
        });

        services.AddAutoMapper(cfg => { }, Assembly.GetExecutingAssembly());

        // JWT
        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                var validAudiences = configuration.GetSection("JWT:ValidAudience").Get<string[]>();
                
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = configuration["JWT:ValidIssuer"],
                    ValidAudiences = validAudiences ?? new[] { 
                        "http://localhost:5222", 
                        "http://localhost:5222",
                        "http://localhost:3000",
                        "http://localhost:5000",
                        "http://localhost:8080",
                        "http://localhost:5222",
                        "http://localhost:5177",
                        "https://localhost:5177"
                    }, 
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(configuration["JWT:SecretKey"])),
                    ClockSkew = TimeSpan.Zero
                };

                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        if (context.Exception is SecurityTokenExpiredException)
                        {
                            context.Response.Headers.Add("Token-Expired", "true");
                        }
                        return Task.CompletedTask;
                    },
                    OnTokenValidated = context =>
                    {
                        return Task.CompletedTask;
                    },
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];
                        var path = context.HttpContext.Request.Path;
                        
                        if (!string.IsNullOrEmpty(accessToken) && 
                            (path.StartsWithSegments("/chatHub") || path.StartsWithSegments("/rentNotificationHub")))
                        {
                            context.Token = accessToken;
                        }
                        return Task.CompletedTask;
                    }
                };
            });

        services.AddAuthorization(ops =>
        {
            ops.AddPolicy("AdminPolicy", policyBuilder => policyBuilder.RequireRole("AppAdmin", "AppSuperAdmin"));
            ops.AddPolicy("UserPolicy", policyBuilder => policyBuilder.RequireRole("User", "AppAdmin"));
        });

        services.AddCors(options =>
        {
            options.AddPolicy("AllowFrontend",
                policy => policy
                    .WithOrigins(
                        "http://localhost:5174",
                        "https://localhost:5174",
                        "http://localhost:5176", 
                        "https://localhost:5176",
                        "http://localhost:5177",
                        "https://localhost:5177",
                        "http://localhost:5173",
                        "https://localhost:5173",
                        "http://26.131.236.226:5174",
                        "https://26.131.236.226:5174",
                        "http://26.131.236.226:5176",
                        "https://26.131.236.226:5176",
                        "http://26.131.236.226:5177",
                        "https://26.131.236.226:5177",
                        "http://26.15.223.176:5173",
                        "https://26.15.223.176:5173",
                        "http://localhost:3000",
                        "https://localhost:3000",
                        "https://cool-fudge-7ca10b.netlify.app"
                    )
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials());
        });

        services.AddScoped<ICloudinaryService, CloudinaryService>();
        services.AddScoped<IEmailSender, SmtpEmailSender>();
        services.AddScoped<IEmailService, EmailService>();

        return services;
    }
}
