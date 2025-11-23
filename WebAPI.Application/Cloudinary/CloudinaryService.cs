using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Configuration;
using System.Net;

namespace WebAPI.Application.Cloudinary;

public class CloudinaryService : ICloudinaryService
{
    private readonly CloudinaryDotNet.Cloudinary _cloudinary;

    public CloudinaryService(IConfiguration configuration)
    {
        var cloudName = configuration["Cloudinary:CloudName"];
        var apiKey = configuration["Cloudinary:ApiKey"];
        var apiSecret = configuration["Cloudinary:ApiSecret"];

        _cloudinary = new CloudinaryDotNet.Cloudinary(new Account(cloudName, apiKey, apiSecret));
    }

    // 🔹 Метод для фото
    public async Task<string?> UploadAsync(Stream fileStream, string fileName) 
    {
        if (fileStream == null || fileStream.Length == 0)
        {
            return null;
        }

        fileStream.Position = 0;

        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription(fileName, fileStream),
            UseFilename = true,
            UniqueFilename = true,
            Overwrite = false
        };

        var uploadResult = await _cloudinary.UploadAsync(uploadParams);

        if (uploadResult.Error != null)
        {
            return null;
        }

        if (uploadResult.StatusCode != HttpStatusCode.OK)
        {
            return null;
        }

        return uploadResult.SecureUrl?.ToString();
    }

    public async Task<string?> UploadVoiceAsync(Stream fileStream, string fileName) 
    {
        if (fileStream == null || fileStream.Length == 0)
        {
            return null;
        }

        fileStream.Position = 0;

        var uploadParams = new VideoUploadParams
        {
            File = new FileDescription(fileName, fileStream),
            UseFilename = true,
            UniqueFilename = true,
            Overwrite = false
        };

        var uploadResult = await _cloudinary.UploadAsync(uploadParams);

        if (uploadResult.Error != null)
        {
            return null;
        }

        if (uploadResult.StatusCode != HttpStatusCode.OK)
        {
            return null;
        }

        return uploadResult.SecureUrl?.ToString();
    }
        
    public async Task<bool> DeleteAsync(string publicId) 
    {
        if (string.IsNullOrWhiteSpace(publicId))
        {
            return false;
        }

        var deletionParams = new DeletionParams(publicId);
        var result = await _cloudinary.DestroyAsync(deletionParams);

        if (result.Error != null)
        {
            return false;
        } 
        return result.Result == "ok";
    }
}