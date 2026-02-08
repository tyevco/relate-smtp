using Relate.Smtp.Api.Services;
using Shouldly;

namespace Relate.Smtp.Tests.Unit.Api.Services;

[Trait("Category", "Unit")]
public class SmtpCredentialServiceTests
{
    private readonly SmtpCredentialService _service;

    public SmtpCredentialServiceTests()
    {
        _service = new SmtpCredentialService();
    }

    [Fact]
    public void GenerateApiKey_ReturnsBase64String()
    {
        // Act
        var key = _service.GenerateApiKey();

        // Assert
        key.ShouldNotBeNullOrEmpty();
        // Base64 strings should be decodable
        var decoded = Convert.FromBase64String(key);
        decoded.Length.ShouldBe(32); // 32 bytes = 256 bits
    }

    [Fact]
    public void GenerateApiKey_GeneratesUniqueKeys()
    {
        // Act
        var keys = Enumerable.Range(0, 100)
            .Select(_ => _service.GenerateApiKey())
            .ToList();

        // Assert
        keys.Distinct().Count().ShouldBe(100);
    }

    [Fact]
    public void HashPassword_ReturnsValidBCryptHash()
    {
        // Arrange
        var password = "TestPassword123!";

        // Act
        var hash = _service.HashPassword(password);

        // Assert
        hash.ShouldNotBeNullOrEmpty();
        hash.ShouldStartWith("$2a$11$"); // BCrypt with work factor 11
    }

    [Fact]
    public void HashPassword_GeneratesUniqueSalts()
    {
        // Arrange
        var password = "TestPassword123!";

        // Act
        var hash1 = _service.HashPassword(password);
        var hash2 = _service.HashPassword(password);

        // Assert
        hash1.ShouldNotBe(hash2); // Same password should produce different hashes due to random salt
    }

    [Fact]
    public void VerifyPassword_ReturnsTrueForCorrectPassword()
    {
        // Arrange
        var password = "TestPassword123!";
        var hash = _service.HashPassword(password);

        // Act
        var result = _service.VerifyPassword(password, hash);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void VerifyPassword_ReturnsFalseForIncorrectPassword()
    {
        // Arrange
        var password = "TestPassword123!";
        var wrongPassword = "WrongPassword456!";
        var hash = _service.HashPassword(password);

        // Act
        var result = _service.VerifyPassword(wrongPassword, hash);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void VerifyPassword_IsCaseSensitive()
    {
        // Arrange
        var password = "TestPassword123!";
        var wrongCasePassword = "testpassword123!";
        var hash = _service.HashPassword(password);

        // Act
        var result = _service.VerifyPassword(wrongCasePassword, hash);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void GeneratedApiKey_CanBeHashedAndVerified()
    {
        // Arrange
        var apiKey = _service.GenerateApiKey();
        var hash = _service.HashPassword(apiKey);

        // Act
        var result = _service.VerifyPassword(apiKey, hash);

        // Assert
        result.ShouldBeTrue();
    }
}
