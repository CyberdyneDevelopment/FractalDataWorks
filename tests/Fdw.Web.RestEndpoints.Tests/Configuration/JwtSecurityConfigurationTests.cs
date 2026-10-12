using Fdw.Web.RestEndpoints.Configuration;

namespace Fdw.Web.RestEndpoints.Tests.Configuration;

public class JwtSecurityConfigurationTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void Constructor_SetsDefaultValues()
    {
        // Arrange & Act
        var config = new JwtSecurityConfiguration();

        // Assert
        config.Enabled.ShouldBeTrue();
        config.Issuer.ShouldBe(string.Empty);
        config.Audience.ShouldBe(string.Empty);
        config.SecretKey.ShouldBe(string.Empty);
        config.ClockSkewSeconds.ShouldBe(300);
        config.ValidateLifetime.ShouldBeTrue();
        config.ValidateIssuer.ShouldBeTrue();
        config.ValidateAudience.ShouldBeTrue();
        config.RequiredClaims.ShouldNotBeNull();
        config.RequiredClaims.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void Properties_CanBeInitialized()
    {
        // Arrange & Act
        var config = new JwtSecurityConfiguration
        {
            Enabled = false,
            Issuer = "test-issuer",
            Audience = "test-audience",
            SecretKey = "test-secret-key",
            ClockSkewSeconds = 60,
            ValidateLifetime = false,
            ValidateIssuer = false,
            ValidateAudience = false,
            RequiredClaims = ["sub", "email"]
        };

        // Assert
        config.Enabled.ShouldBeFalse();
        config.Issuer.ShouldBe("test-issuer");
        config.Audience.ShouldBe("test-audience");
        config.SecretKey.ShouldBe("test-secret-key");
        config.ClockSkewSeconds.ShouldBe(60);
        config.ValidateLifetime.ShouldBeFalse();
        config.ValidateIssuer.ShouldBeFalse();
        config.ValidateAudience.ShouldBeFalse();
        config.RequiredClaims.ShouldBe(new[] { "sub", "email" });
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void Enabled_CanBeSet()
    {
        // Arrange & Act
        var config = new JwtSecurityConfiguration { Enabled = false };

        // Assert
        config.Enabled.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void Issuer_CanBeSet()
    {
        // Arrange & Act
        var config = new JwtSecurityConfiguration { Issuer = "my-issuer" };

        // Assert
        config.Issuer.ShouldBe("my-issuer");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void Audience_CanBeSet()
    {
        // Arrange & Act
        var config = new JwtSecurityConfiguration { Audience = "my-audience" };

        // Assert
        config.Audience.ShouldBe("my-audience");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void SecretKey_CanBeSet()
    {
        // Arrange & Act
        var config = new JwtSecurityConfiguration { SecretKey = "my-secret" };

        // Assert
        config.SecretKey.ShouldBe("my-secret");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ClockSkewSeconds_CanBeSet()
    {
        // Arrange & Act
        var config = new JwtSecurityConfiguration { ClockSkewSeconds = 120 };

        // Assert
        config.ClockSkewSeconds.ShouldBe(120);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ValidateLifetime_CanBeSet()
    {
        // Arrange & Act
        var config = new JwtSecurityConfiguration { ValidateLifetime = false };

        // Assert
        config.ValidateLifetime.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ValidateIssuer_CanBeSet()
    {
        // Arrange & Act
        var config = new JwtSecurityConfiguration { ValidateIssuer = false };

        // Assert
        config.ValidateIssuer.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ValidateAudience_CanBeSet()
    {
        // Arrange & Act
        var config = new JwtSecurityConfiguration { ValidateAudience = false };

        // Assert
        config.ValidateAudience.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void RequiredClaims_CanBeSet()
    {
        // Arrange
        var claims = new[] { "name", "role", "permissions" };

        // Act
        var config = new JwtSecurityConfiguration { RequiredClaims = claims };

        // Assert
        config.RequiredClaims.ShouldBe(claims);
    }
}
