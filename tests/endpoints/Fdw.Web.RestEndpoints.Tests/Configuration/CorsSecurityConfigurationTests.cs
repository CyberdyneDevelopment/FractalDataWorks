using Fdw.Web.RestEndpoints.Configuration;

namespace Fdw.Web.RestEndpoints.Tests.Configuration;

public class CorsSecurityConfigurationTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void Constructor_SetsDefaultValues()
    {
        // Arrange & Act
        var config = new CorsSecurityConfiguration();

        // Assert
        config.Enabled.ShouldBeTrue();
        config.AllowedOrigins.ShouldNotBeNull();
        config.AllowedOrigins.ShouldBeEmpty();
        config.AllowedMethods.ShouldBe(new[] { "GET", "POST", "PUT", "DELETE" });
        config.AllowedHeaders.ShouldBe(new[] { "Content-Type", "Authorization" });
        config.AllowCredentials.ShouldBeFalse();
        config.PreflightMaxAgeSeconds.ShouldBe(86400);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void Properties_CanBeInitialized()
    {
        // Arrange & Act
        var config = new CorsSecurityConfiguration
        {
            Enabled = false,
            AllowedOrigins = ["https://example.com"],
            AllowedMethods = ["GET", "HEAD"],
            AllowedHeaders = ["X-Custom-Header"],
            AllowCredentials = true,
            PreflightMaxAgeSeconds = 3600
        };

        // Assert
        config.Enabled.ShouldBeFalse();
        config.AllowedOrigins.ShouldBe(new[] { "https://example.com" });
        config.AllowedMethods.ShouldBe(new[] { "GET", "HEAD" });
        config.AllowedHeaders.ShouldBe(new[] { "X-Custom-Header" });
        config.AllowCredentials.ShouldBeTrue();
        config.PreflightMaxAgeSeconds.ShouldBe(3600);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void Enabled_CanBeSet()
    {
        // Arrange & Act
        var config = new CorsSecurityConfiguration { Enabled = false };

        // Assert
        config.Enabled.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void AllowedOrigins_CanBeSet()
    {
        // Arrange
        var origins = new[] { "https://app1.com", "https://app2.com" };

        // Act
        var config = new CorsSecurityConfiguration { AllowedOrigins = origins };

        // Assert
        config.AllowedOrigins.ShouldBe(origins);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void AllowedMethods_CanBeSet()
    {
        // Arrange
        var methods = new[] { "GET", "POST", "PATCH" };

        // Act
        var config = new CorsSecurityConfiguration { AllowedMethods = methods };

        // Assert
        config.AllowedMethods.ShouldBe(methods);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void AllowedHeaders_CanBeSet()
    {
        // Arrange
        var headers = new[] { "Accept", "Content-Type" };

        // Act
        var config = new CorsSecurityConfiguration { AllowedHeaders = headers };

        // Assert
        config.AllowedHeaders.ShouldBe(headers);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void AllowCredentials_CanBeSet()
    {
        // Arrange & Act
        var config = new CorsSecurityConfiguration { AllowCredentials = true };

        // Assert
        config.AllowCredentials.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void PreflightMaxAgeSeconds_CanBeSet()
    {
        // Arrange & Act
        var config = new CorsSecurityConfiguration { PreflightMaxAgeSeconds = 7200 };

        // Assert
        config.PreflightMaxAgeSeconds.ShouldBe(7200);
    }
}
