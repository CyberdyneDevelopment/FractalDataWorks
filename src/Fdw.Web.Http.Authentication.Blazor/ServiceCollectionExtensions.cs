namespace Fdw.Web.Http.Authentication.Blazor;

using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

/// <summary>
/// Extension methods for registering Blazor Server authentication services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the Blazor Server circuit token bridge, including the
    /// <see cref="CircuitTokenAccessor"/>, <see cref="TokenCapturingCircuitHandler"/>,
    /// and <see cref="BlazorServerAccessTokenProvider"/>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddBlazorServerAuthentication(this IServiceCollection services)
    {
        services.TryAddSingleton<CircuitTokenAccessor>();
        services.AddScoped<CircuitHandler, TokenCapturingCircuitHandler>();
        services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        services.TryAddScoped<IAccessTokenProvider, BlazorServerAccessTokenProvider>();

        return services;
    }
}
