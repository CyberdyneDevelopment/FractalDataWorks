using Fdw.Data.DataStores.Rest;

namespace Fdw.Data.DataStores.Tests;

/// <summary>
/// Tests for <see cref="RestConfiguration"/>.
/// </summary>
public sealed class RestConfigurationTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void BaseUrlCanBeSet()
    {
        // Arrange
        var sut = new RestConfiguration();

        // Act
        sut.BaseUrl = "https://api.example.com";

        // Assert
        sut.BaseUrl.ShouldBe("https://api.example.com");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void OpenApiSpecUrlCanBeSet()
    {
        // Arrange
        var sut = new RestConfiguration();

        // Act
        sut.OpenApiSpecUrl = "https://api.example.com/swagger.json";

        // Assert
        sut.OpenApiSpecUrl.ShouldBe("https://api.example.com/swagger.json");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void AuthenticationTypeCanBeSetToBearerToken()
    {
        // Arrange
        var sut = new RestConfiguration();

        // Act
        sut.AuthenticationType = "Bearer";

        // Assert
        sut.AuthenticationType.ShouldBe("Bearer");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void AuthenticationTypeCanBeSetToApiKey()
    {
        // Arrange
        var sut = new RestConfiguration();

        // Act
        sut.AuthenticationType = "ApiKey";

        // Assert
        sut.AuthenticationType.ShouldBe("ApiKey");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ApiKeyCanBeSet()
    {
        // Arrange
        var sut = new RestConfiguration();

        // Act
        sut.ApiKey = "my-api-key-12345";

        // Assert
        sut.ApiKey.ShouldBe("my-api-key-12345");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void BearerTokenCanBeSet()
    {
        // Arrange
        var sut = new RestConfiguration();

        // Act
        sut.BearerToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9";

        // Assert
        sut.BearerToken.ShouldBe("eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void FullConfigurationCanBeBuiltWithAllProperties()
    {
        // Arrange & Act
        var sut = new RestConfiguration
        {
            BaseUrl = "https://api.example.com/v1",
            OpenApiSpecUrl = "https://api.example.com/openapi.json",
            AuthenticationType = "Bearer",
            BearerToken = "test-token",
        };

        // Assert
        sut.BaseUrl.ShouldBe("https://api.example.com/v1");
        sut.OpenApiSpecUrl.ShouldBe("https://api.example.com/openapi.json");
        sut.AuthenticationType.ShouldBe("Bearer");
        sut.BearerToken.ShouldBe("test-token");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void MultipleConfigurationsAreIndependent()
    {
        // Arrange
        var config1 = new RestConfiguration { BaseUrl = "https://api1.example.com" };
        var config2 = new RestConfiguration { BaseUrl = "https://api2.example.com" };

        // Assert
        config1.BaseUrl.ShouldNotBe(config2.BaseUrl);
    }
}
