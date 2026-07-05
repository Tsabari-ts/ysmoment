using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Configuration;
using YsMoment.Core.Interfaces;

namespace YsMoment.Infrastructure.Services;

public class CloudinaryImageStorageService : IImageStorageService
{
    private readonly Cloudinary _cloudinary;
    private readonly string _folder;
    private readonly string _cloudName;

    public CloudinaryImageStorageService(IConfiguration configuration)
    {
        _cloudName = configuration["Cloudinary:CloudName"]
            ?? throw new InvalidOperationException("Cloudinary:CloudName is required.");
        var apiKey = configuration["Cloudinary:ApiKey"]
            ?? throw new InvalidOperationException("Cloudinary:ApiKey is required.");
        var apiSecret = configuration["Cloudinary:ApiSecret"]
            ?? throw new InvalidOperationException("Cloudinary:ApiSecret is required.");

        _cloudinary = new Cloudinary(new Account(_cloudName, apiKey, apiSecret));
        _cloudinary.Api.Secure = true;
        _folder = configuration["Cloudinary:Folder"] ?? "ysmoment";
    }

    public async Task<string> SaveAsync(Stream stream, string fileName, Guid eventId, Guid orderId)
    {
        var publicId = $"{_folder}/{eventId}/{orderId}";
        var uploadParams = new RawUploadParams
        {
            File = new FileDescription(fileName, stream),
            PublicId = publicId,
            Overwrite = false,
            Tags = $"event-{eventId}"
        };

        var result = await _cloudinary.UploadAsync(uploadParams);
        if (result.Error != null)
            throw new InvalidOperationException($"Cloudinary upload failed: {result.Error.Message}");

        return result.PublicId;
    }

    public async Task DeleteAsync(string imagePath)
    {
        if (string.IsNullOrEmpty(imagePath)) return;
        var deleteParams = new DeletionParams(imagePath) { ResourceType = ResourceType.Raw };
        await _cloudinary.DestroyAsync(deleteParams);
    }

    public string? GetPublicUrl(string? imagePath)
    {
        if (string.IsNullOrEmpty(imagePath)) return null;
        // Raw resource URL format
        return $"https://res.cloudinary.com/{_cloudName}/raw/upload/{imagePath}";
    }

    // Cloud storage has no physical path — callers must use GetPublicUrl instead
    public string? GetPhysicalPath(string? imagePath) => null;
}
