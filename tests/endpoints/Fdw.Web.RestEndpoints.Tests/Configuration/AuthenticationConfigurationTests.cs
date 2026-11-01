using Fdw.Web.RestEndpoints.Configuration;

namespace Fdw.Web.RestEndpoints.Tests.Configuration;

public class AuthenticationConfigurationTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void Constructor_SetsDefaultValues()
    {
        // Arrange & Act
        var config = new AuthenticationConfiguration();

        // Assert
        config.Jwt.ShouldNotBeNull();
        config.ApiKey.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void Properties_CanBeInitialized()
    {
        // Arrange
        var jwtConfig = new JwtConfiguration { Issuer = "test-issuer" };
        var apiKeyConfig = new ApiKeyConfiguration { HeaderName = "X-Test-Key" };

        // Act
        var config = new AuthenticationConfiguration
        {
            Jwt = jwtConfig,
            ApiKey = apiKeyConfig
        };

        // Assert
        config.Jwt.ShouldBe(jwtConfig);
        config.ApiKey.ShouldBe(apiKeyConfig);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void Jwt_CanBeSet()
    {
        // Arrange
        var jwtConfig = new JwtConfiguration
        {
            Issuer = "my-issuer",
            Audience = "my-audience"
        };

        // Act
        var config = new AuthenticationConfiguration { Jwt = jwtConfig };

        // Assert
        config.Jwt.ShouldBe(jwtConfig);
        config.Jwt.Issuer.ShouldBe("my-issuer");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ApiKey_CanBeSet()
    {
        // Arrange
        var apiKeyConfig = new ApiKeyConfiguration
        {
            HeaderName = "Authorization",
            ValidKeys = ["key1", "key2"]
        };

        // Act
        var config = new AuthenticationConfiguration { ApiKey = apiKeyConfig };

        // Assert
        config.ApiKey.ShouldBe(apiKeyConfig);
        config.ApiKey.HeaderName.ShouldBe("Authorization");
    }
}
