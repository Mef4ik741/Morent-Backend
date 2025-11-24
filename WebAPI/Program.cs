using WebAPI.Extensions;
using Microsoft.AspNetCore.Http.Features;

var builder = WebApplication.CreateBuilder(args);

// Allow binding to multiple URLs (e.g., localhost + 0.0.0.0 + specific IP)
var configuredUrls = builder.Configuration["Urls"];
if (!string.IsNullOrWhiteSpace(configuredUrls))
{
    builder.WebHost.UseUrls(configuredUrls.Split(';', StringSplitOptions.RemoveEmptyEntries));
}
else
{
    var port = Environment.GetEnvironmentVariable("PORT");
    if (!string.IsNullOrWhiteSpace(port))
    {
        builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
    }
}

builder.Services.AddApplicationServices(builder.Configuration);

builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 10 * 1024 * 1024; 
});

var app = builder.Build();

app.UseApplicationMiddleware();

app.Run();