using System;
using Fdw.Services.Audit;
using Fdw.Services.Audit.Abstractions;
using Fdw.Services.Data.Abstractions.Discovery;
using Fdw.Services.Data.Discovery;
using Fdw.UI.Rendering.Spectre.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Fdw.Hosting.Cli;

/// <summary>
/// One-line bootstrap for FDW-based CLI / admin-console hosts. Registers the
/// Spectre UI renderer, the default audit context accessor, and the schema
/// discovery factory so any CLI that consumes FDW providers can stand up with
/// consistent wiring.
/// </summary>
// Why: every CLI built on FDW needs the same chunk of DI registrations — the
// Spectre renderer, an audit accessor (so writes get audited even without
// HttpContext), and the schema-discovery factory for `discover` verbs. Putting
// this behind a single extension avoids each tool reimplementing the dance.
public static class AddFrameworkCliExtensions
{
    /// <summary>
    /// Adds the standard set of services a CLI host needs:
    /// <list type="bullet">
    ///   <item>Spectre.Console rendering (<c>IUIRenderer</c>, <c>SpectreRenderContext</c>, <c>IAnsiConsole</c>).</item>
    ///   <item>Default <see cref="IAuditContextAccessor"/> fallback (<see cref="SystemAuditContextAccessor"/>).</item>
    ///   <item>A <see cref="DefaultSchemaDiscoveryFactory"/> so connection packages can register adapters.</item>
    /// </list>
    /// Callers are free to override any of these afterwards (e.g., replace the
    /// audit accessor with a profile-backed CLI accessor).
    /// </summary>
    public static IServiceCollection AddFrameworkCli(
        this IServiceCollection services,
        int defaultThemeId = 1)
    {
        if (services is null) throw new ArgumentNullException(nameof(services));

        services.AddFrameworkSpectreUI(defaultThemeId);

        // Why: every write path will run through AuditingConfigurationWriter at some
        // point. Registering the system accessor as the default ensures audits still
        // record a deterministic user even before the CLI host supplies a profile-
        // aware implementation.
        services.TryAddSingleton<IAuditContextAccessor, SystemAuditContextAccessor>();

        // Why: DefaultSchemaDiscoveryFactory is a singleton bucket that connection
        // packages (MsSql, PostgreSql, Http) register discoverers into at startup.
        // Register it here so every CLI has the factory ready, and consumers can
        // push registrations on boot.
        services.TryAddSingleton<DefaultSchemaDiscoveryFactory>();
        services.TryAddSingleton<ISchemaDiscoveryFactory>(
            sp => sp.GetRequiredService<DefaultSchemaDiscoveryFactory>());

        return services;
    }
}
