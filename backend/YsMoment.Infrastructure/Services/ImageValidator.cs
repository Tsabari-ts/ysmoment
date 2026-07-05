using YsMoment.Core.Interfaces;

namespace YsMoment.Infrastructure.Services;

public class ImageValidator : IImageValidator
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
        { ".jpg", ".jpeg", ".png", ".heic" };

    private const long MaxFileSize = 10 * 1024 * 1024; // 10 MB

    public (bool IsValid, string? Error) Validate(Stream stream, string fileName, long fileSize)
    {
        var ext = Path.GetExtension(fileName);
        if (string.IsNullOrEmpty(ext) || !AllowedExtensions.Contains(ext))
            return (false, "סוג קובץ לא נתמך. רק JPG, JPEG, PNG, HEIC מותרים.");

        if (fileSize <= 0)
            return (false, "הקובץ ריק.");

        if (fileSize > MaxFileSize)
            return (false, "הקובץ גדול מדי. מקסימום 10MB.");

        if (!IsValidImageSignature(stream, ext))
            return (false, "הקובץ אינו תמונה חוקית.");

        return (true, null);
    }

    private static bool IsValidImageSignature(Stream stream, string ext)
    {
        Span<byte> header = stackalloc byte[12];
        var read = stream.Read(header);
        stream.Position = 0;

        if (read < 3) return false;

        // JPEG
        if (header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF)
            return ext.Equals(".jpg", StringComparison.OrdinalIgnoreCase)
                || ext.Equals(".jpeg", StringComparison.OrdinalIgnoreCase);

        // PNG
        if (read >= 8 && header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47)
            return ext.Equals(".png", StringComparison.OrdinalIgnoreCase);

        // HEIC/HEIF - ftyp box
        if (read >= 12 && header[4] == 0x66 && header[5] == 0x74 && header[6] == 0x79 && header[7] == 0x70)
            return ext.Equals(".heic", StringComparison.OrdinalIgnoreCase);

        return false;
    }
}
