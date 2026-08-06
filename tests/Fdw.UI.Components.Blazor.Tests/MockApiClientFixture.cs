using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace Fdw.UI.Components.Blazor.Tests;

/// <summary>
/// Base fixture for testing UI provider components with mocked API clients.
/// Handles DI setup, component context, and common assertions.
/// </summary>
public abstract class MockApiClientFixture : IDisposable
{
    protected BunitContext BUnitContext { get; }
    protected IServiceProvider ServiceProvider { get; }

    protected MockApiClientFixture()
    {
        BUnitContext = new BunitContext();

        var services = new ServiceCollection();
        ConfigureServices(services);
        ServiceProvider = services.BuildServiceProvider();

        // Register services from the built provider into bUnit's test service collection
        foreach (var descriptor in services)
        {
            BUnitContext.Services.Add(descriptor);
        }
    }

    /// <summary>
    /// Override to configure additional services for your provider tests.
    /// </summary>
    protected virtual void ConfigureServices(IServiceCollection services)
    {
        // Base DI setup — subclasses add their mocked API clients
        services.AddLogging();
    }

    /// <summary>
    /// Create a mock ILogger<T> for assertions and injection.
    /// </summary>
    protected Mock<ILogger<T>> CreateMockLogger<T>()
    {
        return new Mock<ILogger<T>>();
    }

    public void Dispose()
    {
        BUnitContext?.Dispose();
        (ServiceProvider as IDisposable)?.Dispose();
    }
}
