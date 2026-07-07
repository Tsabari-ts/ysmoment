using Microsoft.Extensions.Logging;
using YsMoment.Core.Interfaces;

namespace YsMoment.Infrastructure.Services;

public class ImageValidator : IImageValidator
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
        { ".jpg", ".jpeg", ".png", ".heic", ".heif" };

    private const long MaxFileSize = 10 * 1024 * 1024; // 10 MB

    private readonly ILogger<ImageValidator> _logger;

    public ImageValidator(ILogger<ImageValidator> logger)
    {
        _logger = logger;
    }

    public (bool IsValid, string? Error) Validate(Stream stream, string fileName, long fileSize)
    {
        var ext = Path.GetExtension(fileName);
        if (string.IsNullOrEmpty(ext) || !AllowedExtensions.Contains(ext))
        {
            _logger.LogWarning(
                "Image upload rejected: unsupported extension. FileName={FileName} Ext={Ext} Size={Size}",
                fileName, ext, fileSize);
            return (false, "סוג קובץ לא נתמך. רק JPG, JPEG, PNG, HEIC מותרים.");
        }

        if (fileSize <= 0)
        {
            _logger.LogWarning("Image upload rejected: empty file. FileName={FileName} Ext={Ext}", fileName, ext);
            return (false, "הקובץ ריק.");
        }

        if (fileSize > MaxFileSize)
        {
            _logger.LogWarning(
                "Image upload rejected: file too large. FileName={FileName} Ext={Ext} Size={Size}",
                fileName, ext, fileSize);
            return (false, "הקובץ גדול מדי. מקסימום 10MB.");
        }

        if (!IsValidImageSignature(stream, ext, out var headerHex))
        {
            _logger.LogWarning(
                "Image upload rejected: signature mismatch. FileName={FileName} Ext={Ext} Size={Size} Header={Header}",
                fileName, ext, fileSize, headerHex);
            return (false, "הקובץ אינו תמונה חוקית.");
        }

        return (true, null);
    }

    private static bool IsValidImageSignature(Stream stream, string ext, out string headerHex)
    {
        Span<byte> header = stackalloc byte[12];

        // Stream.Read is not guaranteed to fill the buffer in a single call — it can return
        // a short read even when more data is available (common on buffered/network-backed
        // request streams, especially over slower mobile connections). Looping until the
        // buffer is full or EOF avoids misreading a valid file's signature as invalid.
        var totalRead = 0;
        while (totalRead < header.Length)
        {
            var n = stream.Read(header[totalRead..]);
            if (n == 0) break;
            totalRead += n;
        }
        stream.Position = 0;
        headerHex = Convert.ToHexString(header[..Math.Max(totalRead, 0)]);

        if (totalRead < 3) return false;

        // JPEG
        if (header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF)
            return ext.Equals(".jpg", StringComparison.OrdinalIgnoreCase)
                || ext.Equals(".jpeg", StringComparison.OrdinalIgnoreCase);

        // PNG
        if (totalRead >= 8 && header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47)
            return ext.Equals(".png", StringComparison.OrdinalIgnoreCase);

        // HEIC/HEIF - ftyp box
        if (totalRead >= 12 && header[4] == 0x66 && header[5] == 0x74 && header[6] == 0x79 && header[7] == 0x70)
            return ext.Equals(".heic", StringComparison.OrdinalIgnoreCase)
                || ext.Equals(".heif", StringComparison.OrdinalIgnoreCase);

        return false;
    }
}
