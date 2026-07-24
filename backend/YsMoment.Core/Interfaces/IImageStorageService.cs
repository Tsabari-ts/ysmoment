namespace YsMoment.Core.Interfaces;

public interface IImageStorageService
{
    Task<string> SaveAsync(Stream stream, string fileName, Guid eventId, Guid orderId);
    Task DeleteAsync(string imagePath);

    /// Full-quality URL — use only where the original file is actually needed (printing).
    string? GetOriginalUrl(string? imagePath);

    /// Transformed, lightweight URL for views that just need to display the image (dashboard).
    string? GetPreviewUrl(string? imagePath);
    string? GetPhysicalPath(string? imagePath);
}
