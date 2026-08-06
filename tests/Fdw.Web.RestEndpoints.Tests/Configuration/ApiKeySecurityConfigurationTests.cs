using System;
using System.Collections.Generic;
using Fdw.Web.RestEndpoints.Configuration;

namespace Fdw.Web.RestEndpoints.Tests.Configuration;

public class ApiKeySecurityConfigurationTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void Constructor_SetsDefaultValues()
    {
        // Arrange & Act
        var config = new ApiKeySecurityConfiguration();

        // Assert
        config.Enabled.ShouldBeTrue();
        config.HeaderName.ShouldBe("X-API-Key");
        config.QueryParameterName.ShouldBe("apikey");
        config.AllowQueryParameter.ShouldBeFalse();
        config.ValidKeys.ShouldNotBeNull();
        config.ValidKeys.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void Properties_CanBeInitialized()
    {
        // Arrange
        var validKeys = new Dictionary<string, ApiKeyMetadata>(StringComparer.Ordinal)
        {
            ["key1"] = new ApiKeyMetadata { Name = "Test Key 1" },
            ["key2"] = new ApiKeyMetadata { Name = "Test Key 2" }
        };

        // Act
        var config = new ApiKeySecurityConfiguration
        {
            Enabled = false,
            HeaderName = "X-Custom-API-Key",
            QueryParameterName = "key",
            AllowQueryParameter = true,
            ValidKeys = validKeys
        };

        // Assert
        config.Enabled.ShouldBeFalse();
        config.HeaderName.ShouldBe("X-Custom-API-Key");
        config.QueryParameterName.ShouldBe("key");
        config.AllowQueryParameter.ShouldBeTrue();
        config.ValidKeys.ShouldBe(validKeys);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void Enabled_CanBeSet()
    {
        // Arrange & Act
        var config = new ApiKeySecurityConfiguration { Enabled = false };

        // Assert
        config.Enabled.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void HeaderName_CanBeSet()
    {
        // Arrange & Act
        var config = new ApiKeySecurityConfiguration { HeaderName = "Authorization" };

        // Assert
        config.HeaderName.ShouldBe("Authorization");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void QueryParameterName_CanBeSet()
    {
        // Arrange & Act
        var config = new ApiKeySecurityConfiguration { QueryParameterName = "token" };

        // Assert
        config.QueryParameterName.ShouldBe("token");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void AllowQueryParameter_CanBeSet()
    {
        // Arrange & Act
        var config = new ApiKeySecurityConfiguration { AllowQueryParameter = true };

        // Assert
        config.AllowQueryParameter.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ValidKeys_CanBeSet()
    {
        // Arrange
        var validKeys = new Dictionary<string, ApiKeyMetadata>(StringComparer.Ordinal)
        {
            ["secret-key"] = new ApiKeyMetadata { Name = "Secret Key", IsActive = true }
        };

        // Act
        var config = new ApiKeySecurityConfiguration { ValidKeys = validKeys };

        // Assert
        config.ValidKeys.ShouldBe(validKeys);
        config.ValidKeys.Count.ShouldBe(1);
    }
}
