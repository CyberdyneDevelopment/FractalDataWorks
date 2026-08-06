using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace Fdw.Services.Tests;

[Collection(nameof(ServicesTestCollection))]
public class ServiceRegistrationOptionsTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void Constructor_InitializesWithDefaults()
    {
        // Arrange & Act
        var options = new ServiceRegistrationOptions();

        // Assert
        options.ShouldNotBeNull();
        options.Lifetime.ShouldBe(ServiceLifetime.Transient);
        options.RegisterAsPrimary.ShouldBeTrue();
        options.ConfigurationSection.ShouldBe(string.Empty);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void Lifetime_CanBeSet()
    {
        // Arrange
        var options = new ServiceRegistrationOptions();

        // Act
        options.Lifetime = ServiceLifetime.Singleton;

        // Assert
        options.Lifetime.ShouldBe(ServiceLifetime.Singleton);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void Lifetime_DefaultsToTransient()
    {
        // Arrange & Act
        var options = new ServiceRegistrationOptions();

        // Assert
        options.Lifetime.ShouldBe(ServiceLifetime.Transient);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void Lifetime_SupportsScoped()
    {
        // Arrange
        var options = new ServiceRegistrationOptions();

        // Act
        options.Lifetime = ServiceLifetime.Scoped;

        // Assert
        options.Lifetime.ShouldBe(ServiceLifetime.Scoped);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void RegisterAsPrimary_CanBeSet()
    {
        // Arrange
        var options = new ServiceRegistrationOptions();

        // Act
        options.RegisterAsPrimary = false;

        // Assert
        options.RegisterAsPrimary.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void RegisterAsPrimary_DefaultsToTrue()
    {
        // Arrange & Act
        var options = new ServiceRegistrationOptions();

        // Assert
        options.RegisterAsPrimary.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ConfigurationSection_CanBeSet()
    {
        // Arrange
        var options = new ServiceRegistrationOptions();

        // Act
        options.ConfigurationSection = "MySection";

        // Assert
        options.ConfigurationSection.ShouldBe("MySection");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ConfigurationSection_DefaultsToEmpty()
    {
        // Arrange & Act
        var options = new ServiceRegistrationOptions();

        // Assert
        options.ConfigurationSection.ShouldBe(string.Empty);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void AllProperties_CanBeSetTogether()
    {
        // Arrange & Act
        var options = new ServiceRegistrationOptions
        {
            Lifetime = ServiceLifetime.Scoped,
            RegisterAsPrimary = false,
            ConfigurationSection = "TestSection"
        };

        // Assert
        options.Lifetime.ShouldBe(ServiceLifetime.Scoped);
        options.RegisterAsPrimary.ShouldBeFalse();
        options.ConfigurationSection.ShouldBe("TestSection");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void Lifetime_CanBeChangedAfterInitialization()
    {
        // Arrange
        var options = new ServiceRegistrationOptions
        {
            Lifetime = ServiceLifetime.Transient
        };

        // Act
        options.Lifetime = ServiceLifetime.Singleton;

        // Assert
        options.Lifetime.ShouldBe(ServiceLifetime.Singleton);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void RegisterAsPrimary_CanBeToggledMultipleTimes()
    {
        // Arrange
        var options = new ServiceRegistrationOptions();

        // Act & Assert
        options.RegisterAsPrimary = false;
        options.RegisterAsPrimary.ShouldBeFalse();

        options.RegisterAsPrimary = true;
        options.RegisterAsPrimary.ShouldBeTrue();

        options.RegisterAsPrimary = false;
        options.RegisterAsPrimary.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ConfigurationSection_AcceptsNestedPaths()
    {
        // Arrange
        var options = new ServiceRegistrationOptions();

        // Act
        options.ConfigurationSection = "Parent:Child:GrandChild";

        // Assert
        options.ConfigurationSection.ShouldBe("Parent:Child:GrandChild");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ConfigurationSection_CanBeSetToNull()
    {
        // Arrange
        var options = new ServiceRegistrationOptions
        {
            ConfigurationSection = "TestSection"
        };

        // Act
        options.ConfigurationSection = null!;

        // Assert
        options.ConfigurationSection.ShouldBeNull();
    }
}
