using Fdw.Services.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Fdw.Services.Abstractions.Tests;

public class ServiceLifetimesTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void TransientReturnsTransientLifetime()
    {
        // Act
        var result = ServiceLifetimes.Transient;

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("Transient");
        result.Id.ShouldBe(1);
        result.EnumValue.ShouldBe(ServiceLifetime.Transient);
        result.Description.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ScopedReturnsScopedLifetime()
    {
        // Act
        var result = ServiceLifetimes.Scoped;

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("Scoped");
        result.Id.ShouldBe(2);
        result.EnumValue.ShouldBe(ServiceLifetime.Scoped);
        result.Description.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void SingletonReturnsSingletonLifetime()
    {
        // Act
        var result = ServiceLifetimes.Singleton;

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("Singleton");
        result.Id.ShouldBe(3);
        result.EnumValue.ShouldBe(ServiceLifetime.Singleton);
        result.Description.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ByNameReturnsTransientForTransient()
    {
        // Act
        var result = ServiceLifetimes.ByName("transient");

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("Transient");
        result.EnumValue.ShouldBe(ServiceLifetime.Transient);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ByNameReturnsScopedForScoped()
    {
        // Act
        var result = ServiceLifetimes.ByName("scoped");

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("Scoped");
        result.EnumValue.ShouldBe(ServiceLifetime.Scoped);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ByNameReturnsSingletonForSingleton()
    {
        // Act
        var result = ServiceLifetimes.ByName("singleton");

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("Singleton");
        result.EnumValue.ShouldBe(ServiceLifetime.Singleton);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ByNameIsCaseInsensitive()
    {
        // Act & Assert
        ServiceLifetimes.ByName("TRANSIENT").ShouldNotBeNull();
        ServiceLifetimes.ByName("Transient").ShouldNotBeNull();
        ServiceLifetimes.ByName("SCOPED").ShouldNotBeNull();
        ServiceLifetimes.ByName("Scoped").ShouldNotBeNull();
        ServiceLifetimes.ByName("SINGLETON").ShouldNotBeNull();
        ServiceLifetimes.ByName("Singleton").ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ByNameReturnsNullForUnknownName()
    {
        // Act
        var result = ServiceLifetimes.ByName("unknown");

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ByNameReturnsNullForNull()
    {
        // Act
        var result = ServiceLifetimes.ByName(null);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ByNameReturnsNullForEmptyString()
    {
        // Act
        var result = ServiceLifetimes.ByName(string.Empty);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ByNameReturnsNullForWhitespace()
    {
        // Act
        var result = ServiceLifetimes.ByName("   ");

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void TransientAlwaysReturnsSameInstance()
    {
        // Act
        var first = ServiceLifetimes.Transient;
        var second = ServiceLifetimes.Transient;

        // Assert
        ReferenceEquals(first, second).ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ScopedAlwaysReturnsSameInstance()
    {
        // Act
        var first = ServiceLifetimes.Scoped;
        var second = ServiceLifetimes.Scoped;

        // Assert
        ReferenceEquals(first, second).ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void SingletonAlwaysReturnsSameInstance()
    {
        // Act
        var first = ServiceLifetimes.Singleton;
        var second = ServiceLifetimes.Singleton;

        // Assert
        ReferenceEquals(first, second).ShouldBeTrue();
    }
}
