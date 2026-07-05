namespace YsMoment.Core.Interfaces;

public interface IImageStorageService
{
    Task<string> SaveAsync(Stream stream, string fileName, Guid eventId, Guid orderId);
    Task DeleteAsync(string imagePath);
    string? GetPublicUrl(string? imagePath);
    string? GetPhysicalPath(string? imagePath);
}
