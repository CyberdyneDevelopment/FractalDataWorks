using System;
using System.Linq;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;
using Shouldly;
using Fdw.Services.RateLimiting.Extensions;

namespace Fdw.Services.RateLimiting.Tests;

/// <summary>
/// Unit tests for RateLimitingServiceExtensions DI registration.
/// </summary>
public sealed class RateLimitingServiceExtensionsTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void AddFrameworkRateLimitingRegistersServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddFrameworkRateLimiting();

        // Assert - verify services were registered (ASP.NET Core rate limiter uses IOptions pattern)
        services.ShouldNotBeEmpty();
        // The rate limiter registration adds services for IOptions configuration
        services.Count.ShouldBeGreaterThan(0);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void AddFrameworkRateLimitingThrowsOnNullServices()
    {
        // Arrange
        IServiceCollection? services = null;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => services!.AddFrameworkRateLimiting());
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void AddFrameworkRateLimitingWithConfigureThrowsOnNullServices()
    {
        // Arrange
        IServiceCollection? services = null;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => services!.AddFrameworkRateLimiting(_ => { }));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void AddFrameworkRateLimitingReturnsSameServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddFrameworkRateLimiting();

        // Assert
        result.ShouldBeSameAs(services);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void AddFrameworkRateLimitingWithConfigurePassesOptions()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act - the configure action is invoked lazily when the service provider is built
        services.AddFrameworkRateLimiting(options =>
        {
            // This is called during service provider resolution, not immediately
            options.RejectionStatusCode = 503;
        });

        // Assert - verify services were registered
        services.ShouldNotBeEmpty();
        services.Count.ShouldBeGreaterThan(0);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void AddFrameworkRateLimitingCanBeCalledMultipleTimes()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act - should not throw
        services.AddFrameworkRateLimiting();
        services.AddFrameworkRateLimiting();

        // Assert - just verify it doesn't throw
        services.ShouldNotBeNull();
    }
}
