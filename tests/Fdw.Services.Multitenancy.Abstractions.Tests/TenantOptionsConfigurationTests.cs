using Fdw.Services.Multitenancy.Abstractions;

namespace Fdw.Services.Multitenancy.Abstractions.Tests;

public class TenantOptionsConfigurationTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void ConstructorInitializesEmptyCollections()
    {
        // Act
        var result = new TenantOptionsConfiguration();

        // Assert
        result.EnabledFeatures.ShouldNotBeNull();
        result.CustomSettings.ShouldNotBeNull();
        result.EnabledFeatures.ShouldBeEmpty();
        result.CustomSettings.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void MaxUsersDefaultsToNull()
    {
        // Act
        var result = new TenantOptionsConfiguration();

        // Assert
        result.MaxUsers.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void StorageQuotaBytesDefaultsToNull()
    {
        // Act
        var result = new TenantOptionsConfiguration();

        // Assert
        result.StorageQuotaBytes.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void ApiRateLimitPerMinuteDefaultsToNull()
    {
        // Act
        var result = new TenantOptionsConfiguration();

        // Assert
        result.ApiRateLimitPerMinute.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void MaxUsersCanBeSet()
    {
        // Arrange
        var config = new TenantOptionsConfiguration();

        // Act
        config.MaxUsers = 100;

        // Assert
        config.MaxUsers.ShouldBe(100);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void StorageQuotaBytesCanBeSet()
    {
        // Arrange
        var config = new TenantOptionsConfiguration();

        // Act
        config.StorageQuotaBytes = 1073741824;

        // Assert
        config.StorageQuotaBytes.ShouldBe(1073741824);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void ApiRateLimitPerMinuteCanBeSet()
    {
        // Arrange
        var config = new TenantOptionsConfiguration();

        // Act
        config.ApiRateLimitPerMinute = 60;

        // Assert
        config.ApiRateLimitPerMinute.ShouldBe(60);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void EnabledFeaturesCanBeSet()
    {
        // Arrange
        var config = new TenantOptionsConfiguration();
        var features = new List<string> { "Feature1", "Feature2" };

        // Act
        config.EnabledFeatures = features;

        // Assert
        config.EnabledFeatures.ShouldBe(features);
        config.EnabledFeatures.Count.ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void CustomSettingsCanBeSet()
    {
        // Arrange
        var config = new TenantOptionsConfiguration();
        var settings = new Dictionary<string, string> { { "Key1", "Value1" } };

        // Act
        config.CustomSettings = settings;

        // Assert
        config.CustomSettings.ShouldBe(settings);
        config.CustomSettings.Count.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void ToOptionsCreatesITenantOptionsInstance()
    {
        // Arrange
        var config = new TenantOptionsConfiguration
        {
            MaxUsers = 100,
            StorageQuotaBytes = 1073741824,
            ApiRateLimitPerMinute = 60
        };

        // Act
        var result = config.ToOptions();

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeAssignableTo<ITenantOptions>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void ToOptionsCopiesMaxUsers()
    {
        // Arrange
        var config = new TenantOptionsConfiguration { MaxUsers = 100 };

        // Act
        var result = config.ToOptions();

        // Assert
        result.MaxUsers.ShouldBe(100);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void ToOptionsCopiesStorageQuotaBytes()
    {
        // Arrange
        var config = new TenantOptionsConfiguration { StorageQuotaBytes = 1073741824 };

        // Act
        var result = config.ToOptions();

        // Assert
        result.StorageQuotaBytes.ShouldBe(1073741824);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void ToOptionsCopiesApiRateLimitPerMinute()
    {
        // Arrange
        var config = new TenantOptionsConfiguration { ApiRateLimitPerMinute = 60 };

        // Act
        var result = config.ToOptions();

        // Assert
        result.ApiRateLimitPerMinute.ShouldBe(60);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void ToOptionsCopiesEnabledFeatures()
    {
        // Arrange
        var config = new TenantOptionsConfiguration
        {
            EnabledFeatures = new List<string> { "Feature1", "Feature2" }
        };

        // Act
        var result = config.ToOptions();

        // Assert
        result.EnabledFeatures.Count().ShouldBe(2);
        result.HasFeature("Feature1").ShouldBeTrue();
        result.HasFeature("Feature2").ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void ToOptionsCopiesCustomSettings()
    {
        // Arrange
        var config = new TenantOptionsConfiguration
        {
            CustomSettings = new Dictionary<string, string>
            {
                { "Key1", "Value1" },
                { "Key2", "Value2" }
            }
        };

        // Act
        var result = config.ToOptions();

        // Assert
        result.CustomSettings.Count.ShouldBe(2);
        result.CustomSettings["Key1"].ShouldBe("Value1");
        result.CustomSettings["Key2"].ShouldBe("Value2");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void ToOptionsHandlesNullablePropertiesWithNull()
    {
        // Arrange
        var config = new TenantOptionsConfiguration
        {
            MaxUsers = null,
            StorageQuotaBytes = null,
            ApiRateLimitPerMinute = null
        };

        // Act
        var result = config.ToOptions();

        // Assert
        result.MaxUsers.ShouldBeNull();
        result.StorageQuotaBytes.ShouldBeNull();
        result.ApiRateLimitPerMinute.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void ToOptionsHandlesEmptyCollections()
    {
        // Arrange
        var config = new TenantOptionsConfiguration
        {
            EnabledFeatures = new List<string>(),
            CustomSettings = new Dictionary<string, string>()
        };

        // Act
        var result = config.ToOptions();

        // Assert
        result.EnabledFeatures.ShouldBeEmpty();
        result.CustomSettings.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void CustomSettingsUseCaseSensitiveComparison()
    {
        // Arrange
        var config = new TenantOptionsConfiguration();
        config.CustomSettings.Add("Key", "Value1");
        config.CustomSettings.Add("key", "Value2");

        // Act & Assert
        config.CustomSettings["Key"].ShouldBe("Value1");
        config.CustomSettings["key"].ShouldBe("Value2");
        config.CustomSettings.Count.ShouldBe(2);
    }
}
