using System;
using Fdw.Services.Data.Abstractions.Discovery;
using Fdw.Services.Data.Discovery;
using Fdw.UI.Rendering.Spectre.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Fdw.Hosting.Cli;

/// <summary>
/// One-line bootstrap for FDW-based CLI / admin-console hosts. Registers the
/// Spectre UI renderer and the schema discovery factory so any CLI that consumes
/// FDW providers can stand up with consistent wiring.
/// </summary>
public static class AddFrameworkCliExtensions
{
    /// <summary>
    /// Adds the standard set of services a CLI host needs:
    /// <list type="bullet">
    ///   <item>Spectre.Console rendering (<c>IUIRenderer</c>, <c>SpectreRenderContext</c>, <c>IAnsiConsole</c>).</item>
    ///   <item>A <see cref="DefaultSchemaDiscoveryFactory"/> so connection packages can register adapters.</item>
    /// </list>
    /// Callers are free to override any of these afterwards.
    /// </summary>
    public static IServiceCollection AddFrameworkCli(
        this IServiceCollection services,
        int defaultThemeId = 1)
    {
        if (services is null) throw new ArgumentNullException(nameof(services));

        services.AddFrameworkSpectreUI(defaultThemeId);

        services.TryAddSingleton<DefaultSchemaDiscoveryFactory>();
        services.TryAddSingleton<ISchemaDiscoveryFactory>(
            sp => sp.GetRequiredService<DefaultSchemaDiscoveryFactory>());

        return services;
    }
}
