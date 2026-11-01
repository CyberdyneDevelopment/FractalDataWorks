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
    public void DefaultConstructorInitializesDefaultValues()
    {
        // Arrange & Act
        var sut = new RestConfiguration();

        // Assert
        sut.BaseUrl.ShouldBe(string.Empty);
        sut.OpenApiSpecUrl.ShouldBeNull();
        sut.AuthenticationType.ShouldBeNull();
        sut.ApiKey.ShouldBeNull();
        sut.BearerToken.ShouldBeNull();
        sut.TimeoutSeconds.ShouldBe(30);
        sut.EnableRetries.ShouldBeTrue();
        sut.MaxRetries.ShouldBe(3);
    }

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
    public void TimeoutSecondsCanBeOverridden()
    {
        // Arrange
        var sut = new RestConfiguration();

        // Act
        sut.TimeoutSeconds = 60;

        // Assert
        sut.TimeoutSeconds.ShouldBe(60);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void EnableRetriesCanBeDisabled()
    {
        // Arrange
        var sut = new RestConfiguration();

        // Act
        sut.EnableRetries = false;

        // Assert
        sut.EnableRetries.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void MaxRetriesCanBeOverridden()
    {
        // Arrange
        var sut = new RestConfiguration();

        // Act
        sut.MaxRetries = 5;

        // Assert
        sut.MaxRetries.ShouldBe(5);
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
            TimeoutSeconds = 45,
            EnableRetries = true,
            MaxRetries = 5
        };

        // Assert
        sut.BaseUrl.ShouldBe("https://api.example.com/v1");
        sut.OpenApiSpecUrl.ShouldBe("https://api.example.com/openapi.json");
        sut.AuthenticationType.ShouldBe("Bearer");
        sut.BearerToken.ShouldBe("test-token");
        sut.TimeoutSeconds.ShouldBe(45);
        sut.EnableRetries.ShouldBeTrue();
        sut.MaxRetries.ShouldBe(5);
    }

    [Theory]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(100)]
    [InlineData(300)]
    public void TimeoutSecondsAcceptsVariousPositiveValues(int timeout)
    {
        // Arrange
        var sut = new RestConfiguration();

        // Act
        sut.TimeoutSeconds = timeout;

        // Assert
        sut.TimeoutSeconds.ShouldBe(timeout);
    }

    [Theory]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(10)]
    public void MaxRetriesAcceptsVariousNonNegativeValues(int maxRetries)
    {
        // Arrange
        var sut = new RestConfiguration();

        // Act
        sut.MaxRetries = maxRetries;

        // Assert
        sut.MaxRetries.ShouldBe(maxRetries);
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
        config1.TimeoutSeconds.ShouldBe(config2.TimeoutSeconds);
    }
}
