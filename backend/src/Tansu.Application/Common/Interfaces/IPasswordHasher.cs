namespace Tansu.Application.Common.Interfaces;

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
    string GenerateTemporaryPassword(int length = 12);
}
