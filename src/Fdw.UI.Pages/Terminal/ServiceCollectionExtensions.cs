using Microsoft.Extensions.DependencyInjection;

namespace Fdw.Services.Terminal.Components;

/// <summary>
/// Extension methods for registering Terminal component services in the DI container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all services required by the Fdw Terminal components library.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <returns>The same <paramref name="services"/> instance for chaining.</returns>
    public static IServiceCollection AddTerminalComponents(this IServiceCollection services)
    {
        // No additional registrations are required at this time.
        // HeadlessTerminal and XTermTerminal rely on ITerminalService, ILogger<T>, and IJSRuntime
        // which are provided by the host (e.g. Blazor Server / WASM infrastructure).
        return services;
    }
}
