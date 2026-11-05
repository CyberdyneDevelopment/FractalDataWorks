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

        // Assert
        configuration.BaseUrl.ShouldBe("https://api.example.com");
        configuration.OpenApiSpecUrl.ShouldBe("https://api.example.com/openapi.json");
        configuration.AuthenticationType.ShouldBe("Bearer");
        configuration.ApiKey.ShouldBe("key-123");
        configuration.BearerToken.ShouldBe("token-456");
    }
}
