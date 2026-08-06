using Fdw.ServiceTypes;
using Microsoft.Extensions.DependencyInjection;

namespace Fdw.Services.Abstractions.Tests;

public class RegistrationOptionsTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ConstructorSetsDefaultLifetimeToScoped()
    {
        // Act
        var options = new RegistrationOptions();

        // Assert
        options.Lifetime.ShouldBe(ServiceLifetime.Scoped);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ConstructorAcceptsLifetimeParameter()
    {
        // Act
        var options = new RegistrationOptions(ServiceLifetime.Singleton);

        // Assert
        options.Lifetime.ShouldBe(ServiceLifetime.Singleton);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void RegisterAsPrimaryDefaultsToTrue()
    {
        // Act
        var options = new RegistrationOptions();

        // Assert
        options.RegisterAsPrimary.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ConfigurationSectionDefaultsToEmpty()
    {
        // Act
        var options = new RegistrationOptions();

        // Assert
        options.ConfigurationSection.ShouldBe(string.Empty);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void RequiredServicesDefaultsToEmptyArray()
    {
        // Act
        var options = new RegistrationOptions();

        // Assert
        options.RequiredServices.ShouldNotBeNull();
        options.RequiredServices.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void RequiredDomainsDefaultsToEmptyArray()
    {
        // Act
        var options = new RegistrationOptions();

        // Assert
        options.RequiredDomains.ShouldNotBeNull();
        options.RequiredDomains.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void RegisterAsCanBeSet()
    {
        // Arrange
        var options = new RegistrationOptions();
        var type = typeof(string);

        // Act
        options.RegisterAs = type;

        // Assert
        options.RegisterAs.ShouldBe(type);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void LifetimeCanBeSet()
    {
        // Arrange
        var options = new RegistrationOptions();

        // Act
        options.Lifetime = ServiceLifetime.Transient;

        // Assert
        options.Lifetime.ShouldBe(ServiceLifetime.Transient);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void RegisterAsPrimaryCanBeSet()
    {
        // Arrange
        var options = new RegistrationOptions();

        // Act
        options.RegisterAsPrimary = false;

        // Assert
        options.RegisterAsPrimary.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ConfigurationSectionCanBeSet()
    {
        // Arrange
        var options = new RegistrationOptions();

        // Act
        options.ConfigurationSection = "MySection";

        // Assert
        options.ConfigurationSection.ShouldBe("MySection");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void RequiredServicesCanBeSet()
    {
        // Arrange
        var options = new RegistrationOptions();
        var services = new[] { typeof(string), typeof(int) };

        // Act
        options.RequiredServices = services;

        // Assert
        options.RequiredServices.ShouldBe(services);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void RequiredDomainsCanBeSet()
    {
        // Arrange
        var options = new RegistrationOptions();
        var domains = new[] { typeof(string), typeof(int) };

        // Act
        options.RequiredDomains = domains;

        // Assert
        options.RequiredDomains.ShouldBe(domains);
    }
}
