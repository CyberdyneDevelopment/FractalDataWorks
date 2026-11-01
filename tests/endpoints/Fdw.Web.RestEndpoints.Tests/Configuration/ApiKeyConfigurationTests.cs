using Fdw.Web.RestEndpoints.Configuration;

namespace Fdw.Web.RestEndpoints.Tests.Configuration;

public class ApiKeyConfigurationTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void Constructor_SetsDefaultValues()
    {
        // Arrange & Act
        var config = new ApiKeyConfiguration();

        // Assert
        config.HeaderName.ShouldBe("X-API-Key");
        config.ValidKeys.ShouldNotBeNull();
        config.ValidKeys.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void Properties_CanBeInitialized()
    {
        // Arrange & Act
        var config = new ApiKeyConfiguration
        {
            HeaderName = "X-Custom-Key",
            ValidKeys = ["key1", "key2", "key3"]
        };

        // Assert
        config.HeaderName.ShouldBe("X-Custom-Key");
        config.ValidKeys.ShouldBe(new[] { "key1", "key2", "key3" });
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void HeaderName_CanBeSet()
    {
        // Arrange & Act
        var config = new ApiKeyConfiguration { HeaderName = "Authorization" };

        // Assert
        config.HeaderName.ShouldBe("Authorization");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ValidKeys_CanBeSet()
    {
        // Arrange & Act
        var keys = new[] { "test-key-1", "test-key-2" };
        var config = new ApiKeyConfiguration { ValidKeys = keys };

        // Assert
        config.ValidKeys.ShouldBe(keys);
    }
}
