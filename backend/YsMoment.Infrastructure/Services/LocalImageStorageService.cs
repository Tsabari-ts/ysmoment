using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using YsMoment.Core.Interfaces;

namespace YsMoment.Infrastructure.Services;

public class LocalImageStorageService : IImageStorageService
{
    private readonly string _basePath;
    private readonly string _baseUrl;

    public LocalImageStorageService(IConfiguration configuration, IHostEnvironment env)
    {
        var path = configuration["Storage:Path"] ?? "uploads";
        if (!Path.IsPathRooted(path))
            path = Path.Combine(env.ContentRootPath, path);
        _basePath = path;
        _baseUrl = configuration["Storage:BaseUrl"] ?? "/uploads";
        Directory.CreateDirectory(_basePath);
    }

    public async Task<string> SaveAsync(Stream stream, string fileName, Guid eventId, Guid orderId)
    {
        var ext = Path.GetExtension(fileName);
        var relativePath = Path.Combine(eventId.ToString(), $"{orderId}{ext}");
        var fullPath = Path.Combine(_basePath, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        await using var file = File.Create(fullPath);
        await stream.CopyToAsync(file);

        return relativePath.Replace('\\', '/');
    }

    public Task DeleteAsync(string imagePath)
    {
        var fullPath = Path.Combine(_basePath, imagePath);
        if (File.Exists(fullPath))
            File.Delete(fullPath);
        return Task.CompletedTask;
    }

    // No local transformation pipeline — dev serves the same file for both original and preview.
    public string? GetOriginalUrl(string? imagePath)
    {
        if (string.IsNullOrEmpty(imagePath)) return null;
        return $"{_baseUrl.TrimEnd('/')}/{imagePath.Replace('\\', '/')}";
    }

    public string? GetPreviewUrl(string? imagePath) => GetOriginalUrl(imagePath);

    public string? GetPhysicalPath(string? imagePath)
    {
        if (string.IsNullOrEmpty(imagePath)) return null;
        var fullPath = Path.Combine(_basePath, imagePath.Replace('/', Path.DirectorySeparatorChar));
        return File.Exists(fullPath) ? fullPath : null;
    }
}
