using QRCoder;

namespace YsMoment.Api.Services;

public class QrCodeService
{
    public string GenerateBase64(string url)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q);
        var qr = new PngByteQRCode(data);
        var bytes = qr.GetGraphic(10);
        return Convert.ToBase64String(bytes);
    }
}
