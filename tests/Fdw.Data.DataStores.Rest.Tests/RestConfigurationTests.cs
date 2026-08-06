using Xunit;
using Shouldly;

namespace Fdw.Data.DataStores.Rest.Tests;

/// <summary>
/// Tests for <see cref="RestConfiguration"/> default values.
/// </summary>
public sealed class RestConfigurationTests
{
    [Fact]
    [Trait("Priority", "P3")]
    [Trait("Category", "Configuration")]
    public void DefaultsMatchDocumentedValues()
    {
        // Act
        var configuration = new RestConfiguration();

        // Assert
        configuration.BaseUrl.ShouldBe(string.Empty);
        configuration.OpenApiSpecUrl.ShouldBeNull();
        configuration.AuthenticationType.ShouldBeNull();
        configuration.ApiKey.ShouldBeNull();
        configuration.BearerToken.ShouldBeNull();
        configuration.TimeoutSeconds.ShouldBe(30);
        configuration.EnableRetries.ShouldBeTrue();
        configuration.MaxRetries.ShouldBe(3);
    }

    [Fact]
    [Trait("Priority", "P3")]
    [Trait("Category", "Configuration")]
    public void PropertiesAreSettable()
    {
        // Arrange
        var configuration = new RestConfiguration();

        // Act
        configuration.BaseUrl = "https://api.example.com";
        configuration.OpenApiSpecUrl = "https://api.example.com/openapi.json";
        configuration.AuthenticationType = "Bearer";
        configuration.ApiKey = "key-123";
        configuration.BearerToken = "token-456";
        configuration.TimeoutSeconds = 60;
        configuration.EnableRetries = false;
        configuration.MaxRetries = 5;

        // Assert
        configuration.BaseUrl.ShouldBe("https://api.example.com");
        configuration.OpenApiSpecUrl.ShouldBe("https://api.example.com/openapi.json");
        configuration.AuthenticationType.ShouldBe("Bearer");
        configuration.ApiKey.ShouldBe("key-123");
        configuration.BearerToken.ShouldBe("token-456");
        configuration.TimeoutSeconds.ShouldBe(60);
        configuration.EnableRetries.ShouldBeFalse();
        configuration.MaxRetries.ShouldBe(5);
    }
}
