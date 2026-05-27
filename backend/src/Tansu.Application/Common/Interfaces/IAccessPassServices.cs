namespace Tansu.Application.Common.Interfaces;

public interface IAccessPassQrEncoder
{
    byte[] EncodePng(string payload, int pixelsPerModule = 8);
}

public interface IAccessPassTokenGenerator
{
    string GenerateToken();
}
