using FluentAssertions;
using Tansu.Infrastructure.Auth;

namespace Tansu.UnitTests;

public class PasswordHasherTests
{
    private readonly PasswordHasher _sut = new();

    [Fact]
    public void Hash_then_Verify_returns_true()
    {
        var hash = _sut.Hash("TestPass1!");
        _sut.Verify("TestPass1!", hash).Should().BeTrue();
    }

    [Fact]
    public void Verify_returns_false_for_wrong_password()
    {
        var hash = _sut.Hash("TestPass1!");
        _sut.Verify("nope", hash).Should().BeFalse();
    }

    [Fact]
    public void GenerateTemporaryPassword_has_requested_length()
    {
        var pwd = _sut.GenerateTemporaryPassword(16);
        pwd.Length.Should().Be(16);
    }

    [Fact]
    public void GenerateTemporaryPassword_enforces_minimum_length()
    {
        var pwd = _sut.GenerateTemporaryPassword(4);
        pwd.Length.Should().BeGreaterThanOrEqualTo(8);
    }
}
