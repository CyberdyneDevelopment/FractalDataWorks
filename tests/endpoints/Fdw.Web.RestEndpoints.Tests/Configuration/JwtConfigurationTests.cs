using Fdw.Web.RestEndpoints.Configuration;

namespace Fdw.Web.RestEndpoints.Tests.Configuration;

public class JwtConfigurationTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void Constructor_SetsDefaultValues()
    {
        // Arrange & Act
        var config = new JwtConfiguration();

        // Assert
        config.Issuer.ShouldBe(string.Empty);
        config.Audience.ShouldBe(string.Empty);
        config.SecretKey.ShouldBe(string.Empty);
        config.ExpirationMinutes.ShouldBe(60);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void Properties_CanBeInitialized()
    {
        // Arrange & Act
        var config = new JwtConfiguration
        {
            Issuer = "my-issuer",
            Audience = "my-audience",
            SecretKey = "my-super-secret-key-that-is-very-long",
            ExpirationMinutes = 120
        };

        // Assert
        config.Issuer.ShouldBe("my-issuer");
        config.Audience.ShouldBe("my-audience");
        config.SecretKey.ShouldBe("my-super-secret-key-that-is-very-long");
        config.ExpirationMinutes.ShouldBe(120);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void Issuer_CanBeSet()
    {
        // Arrange & Act
        var config = new JwtConfiguration { Issuer = "test-issuer" };

        // Assert
        config.Issuer.ShouldBe("test-issuer");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void Audience_CanBeSet()
    {
        // Arrange & Act
        var config = new JwtConfiguration { Audience = "test-audience" };

        // Assert
        config.Audience.ShouldBe("test-audience");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void SecretKey_CanBeSet()
    {
        // Arrange & Act
        var config = new JwtConfiguration { SecretKey = "secret-123" };

        // Assert
        config.SecretKey.ShouldBe("secret-123");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ExpirationMinutes_CanBeSet()
    {
        // Arrange & Act
        var config = new JwtConfiguration { ExpirationMinutes = 30 };

        // Assert
        config.ExpirationMinutes.ShouldBe(30);
    }
}
