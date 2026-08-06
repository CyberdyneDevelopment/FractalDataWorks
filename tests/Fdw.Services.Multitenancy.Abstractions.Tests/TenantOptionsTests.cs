using Fdw.Services.Multitenancy.Abstractions;

namespace Fdw.Services.Multitenancy.Abstractions.Tests;

public class TenantOptionsTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void ConstructorInitializesEmptyCollections()
    {
        // Act
        var result = new TenantOptions();

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
        var result = new TenantOptions();

        // Assert
        result.MaxUsers.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void StorageQuotaBytesDefaultsToNull()
    {
        // Act
        var result = new TenantOptions();

        // Assert
        result.StorageQuotaBytes.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void ApiRateLimitPerMinuteDefaultsToNull()
    {
        // Act
        var result = new TenantOptions();

        // Assert
        result.ApiRateLimitPerMinute.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void MaxUsersCanBeSet()
    {
        // Arrange
        var options = new TenantOptions();

        // Act
        options.MaxUsers = 100;

        // Assert
        options.MaxUsers.ShouldBe(100);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void StorageQuotaBytesCanBeSet()
    {
        // Arrange
        var options = new TenantOptions();

        // Act
        options.StorageQuotaBytes = 1073741824;

        // Assert
        options.StorageQuotaBytes.ShouldBe(1073741824);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void ApiRateLimitPerMinuteCanBeSet()
    {
        // Arrange
        var options = new TenantOptions();

        // Act
        options.ApiRateLimitPerMinute = 60;

        // Assert
        options.ApiRateLimitPerMinute.ShouldBe(60);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void AddFeatureAddsToCollection()
    {
        // Arrange
        var options = new TenantOptions();

        // Act
        options.AddFeature("AdvancedReporting");

        // Assert
        options.EnabledFeatures.ShouldContain("AdvancedReporting");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void AddFeatureSupportsCaseInsensitiveComparison()
    {
        // Arrange
        var options = new TenantOptions();
        options.AddFeature("AdvancedReporting");

        // Act & Assert
        options.HasFeature("advancedreporting").ShouldBeTrue();
        options.HasFeature("ADVANCEDREPORTING").ShouldBeTrue();
        options.HasFeature("AdvancedReporting").ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void HasFeatureReturnsFalseForNonexistentFeature()
    {
        // Arrange
        var options = new TenantOptions();

        // Act
        var result = options.HasFeature("NonexistentFeature");

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void HasFeatureReturnsTrueForAddedFeature()
    {
        // Arrange
        var options = new TenantOptions();
        options.AddFeature("TestFeature");

        // Act
        var result = options.HasFeature("TestFeature");

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void SetSettingAddsToCustomSettings()
    {
        // Arrange
        var options = new TenantOptions();

        // Act
        options.SetSetting("Theme", "Dark");

        // Assert
        options.CustomSettings["Theme"].ShouldBe("Dark");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void SetSettingOverwritesExistingValue()
    {
        // Arrange
        var options = new TenantOptions();
        options.SetSetting("Theme", "Light");

        // Act
        options.SetSetting("Theme", "Dark");

        // Assert
        options.CustomSettings["Theme"].ShouldBe("Dark");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void CustomSettingsUseCaseSensitiveComparison()
    {
        // Arrange
        var options = new TenantOptions();
        options.SetSetting("Theme", "Dark");
        options.SetSetting("theme", "Light");

        // Act & Assert
        options.CustomSettings["Theme"].ShouldBe("Dark");
        options.CustomSettings["theme"].ShouldBe("Light");
        options.CustomSettings.Count.ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void DefaultReturnsSharedInstance()
    {
        // Act
        var first = TenantOptions.Default;
        var second = TenantOptions.Default;

        // Assert
        first.ShouldBeSameAs(second);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void DefaultHasEmptyCollections()
    {
        // Act
        var result = TenantOptions.Default;

        // Assert
        result.EnabledFeatures.ShouldBeEmpty();
        result.CustomSettings.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void ImplementsITenantOptions()
    {
        // Act
        var result = new TenantOptions();

        // Assert
        result.ShouldBeAssignableTo<ITenantOptions>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void EnabledFeaturesReturnsReadableCollection()
    {
        // Arrange
        var options = new TenantOptions();
        options.AddFeature("Feature1");
        options.AddFeature("Feature2");

        // Act
        var features = options.EnabledFeatures.ToList();

        // Assert
        features.Count.ShouldBe(2);
        features.ShouldContain("Feature1");
        features.ShouldContain("Feature2");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void CustomSettingsReturnsReadOnlyDictionary()
    {
        // Arrange
        var options = new TenantOptions();
        options.SetSetting("Key1", "Value1");
        options.SetSetting("Key2", "Value2");

        // Act
        var settings = options.CustomSettings;

        // Assert
        settings.Count.ShouldBe(2);
        settings["Key1"].ShouldBe("Value1");
        settings["Key2"].ShouldBe("Value2");
    }
}
