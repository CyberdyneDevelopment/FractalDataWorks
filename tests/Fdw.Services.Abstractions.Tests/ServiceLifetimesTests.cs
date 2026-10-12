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
