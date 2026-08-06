using Fdw.Web.RestEndpoints.Configuration;

namespace Fdw.Web.RestEndpoints.Tests.Configuration;

public class CorsConfigurationTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void Constructor_SetsDefaultValues()
    {
        // Arrange & Act
        var config = new CorsConfiguration();

        // Assert
        config.Enabled.ShouldBeTrue();
        config.AllowedOrigins.ShouldBe(new[] { "*" });
        config.AllowedMethods.ShouldBe(new[] { "GET", "POST", "PUT", "DELETE", "PATCH", "OPTIONS" });
        config.AllowedHeaders.ShouldBe(new[] { "*" });
        config.AllowCredentials.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void Properties_CanBeInitialized()
    {
        // Arrange & Act
        var config = new CorsConfiguration
        {
            Enabled = false,
            AllowedOrigins = ["https://example.com", "https://test.com"],
            AllowedMethods = ["GET", "POST"],
            AllowedHeaders = ["Content-Type", "Authorization"],
            AllowCredentials = true
        };

        // Assert
        config.Enabled.ShouldBeFalse();
        config.AllowedOrigins.ShouldBe(new[] { "https://example.com", "https://test.com" });
        config.AllowedMethods.ShouldBe(new[] { "GET", "POST" });
        config.AllowedHeaders.ShouldBe(new[] { "Content-Type", "Authorization" });
        config.AllowCredentials.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void Enabled_CanBeSet()
    {
        // Arrange & Act
        var config = new CorsConfiguration { Enabled = false };

        // Assert
        config.Enabled.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void AllowedOrigins_CanBeSet()
    {
        // Arrange
        var origins = new[] { "https://localhost:3000", "https://app.example.com" };

        // Act
        var config = new CorsConfiguration { AllowedOrigins = origins };

        // Assert
        config.AllowedOrigins.ShouldBe(origins);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void AllowedMethods_CanBeSet()
    {
        // Arrange
        var methods = new[] { "GET", "HEAD", "OPTIONS" };

        // Act
        var config = new CorsConfiguration { AllowedMethods = methods };

        // Assert
        config.AllowedMethods.ShouldBe(methods);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void AllowedHeaders_CanBeSet()
    {
        // Arrange
        var headers = new[] { "X-Custom-Header", "Accept" };

        // Act
        var config = new CorsConfiguration { AllowedHeaders = headers };

        // Assert
        config.AllowedHeaders.ShouldBe(headers);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void AllowCredentials_CanBeSet()
    {
        // Arrange & Act
        var config = new CorsConfiguration { AllowCredentials = true };

        // Assert
        config.AllowCredentials.ShouldBeTrue();
    }
}
