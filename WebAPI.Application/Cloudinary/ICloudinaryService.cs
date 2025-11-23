namespace WebAPI.Application.Cloudinary;
public interface ICloudinaryService 
{ 
    Task<string?> UploadAsync(Stream fileStream, string fileName); 
    Task<string?> UploadVoiceAsync(Stream fileStream, string fileName);
    Task<bool> DeleteAsync(string publicId);
}
