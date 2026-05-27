using QRCoder;
using Tansu.Application.Common.Interfaces;

namespace Tansu.Infrastructure.AccessPasses;

public sealed class AccessPassQrEncoder : IAccessPassQrEncoder
{
    public byte[] EncodePng(string payload, int pixelsPerModule = 8)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.Q);
        var qr = new PngByteQRCode(data);
        return qr.GetGraphic(pixelsPerModule);
    }
}
