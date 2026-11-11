using System;
using Fdw.Collections;
using Fdw.Results;
using Fdw.TUI.Management.Navigation;
using Fdw.TUI.Management.Screens;
using Fdw.TUI.Management.Services;
using Fdw.TUI.Management.Services.Api;
using Fdw.UI.Abstractions.Rendering;
using Fdw.UI.Rendering.Spectre;
using Fdw.UI.Themes;
using Fdw.Web.Clients.Abstractions.Registration;
using Fdw.Web.Http.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace Fdw.TUI.Management;

/// <summary>
/// Registers the management console's services through the PlatformServices phases (FDW-3957), so the
/// Fdw.TUI.Management executable registers nothing itself (FDW056).
/// </summary>
[PlatformServiceProvider(ServiceCategory = "TuiManagement")]
public sealed class ManagementAppHosting
{
    /// <summary>The configuration key that names the console's menu theme.</summary>
    public const string MenuThemeKey = "Tui:Theme";

    /// <summary>Configure phase: nothing to bind.</summary>
    /// <param name="builder">The host builder.</param>
    /// <param name="loggerFactory">The logger factory.</param>
    /// <param name="force">Whether to run again.</param>
    /// <param name="defer">Whether the host runs this phase at a position it chooses.</param>
    /// <returns>The builder.</returns>
    public static IGenericResult<IHostApplicationBuilder> Configure(IHostApplicationBuilder builder, ILoggerFactory? loggerFactory = null, bool force = false, bool defer = false)
        => GenericResult<IHostApplicationBuilder>.Success(builder);

    /// <summary>Register phase: the console, its navigation, rendering, screens, services and API client seams.</summary>
    /// <param name="builder">The host builder.</param>
    /// <param name="loggerFactory">The logger factory.</param>
    /// <param name="force">Whether to run again.</param>
    /// <param name="defer">Whether the host runs this phase at a position it chooses.</param>
    /// <returns>The builder.</returns>
    public static IGenericResult<IHostApplicationBuilder> Register(IHostApplicationBuilder builder, ILoggerFactory? loggerFactory = null, bool force = false, bool defer = false)
    {
        // Why: defer is the host claiming this phase to run at a position it chooses.
        // Stateless, so there is no flag to set - returning is the whole of it.
        if (defer)
        {
            return GenericResult<IHostApplicationBuilder>.Success(builder);
        }

        var services = builder.Services;

        services.AddSingleton<ManagementApp>();
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<IScreenFactory, ScreenFactory>();
        services.AddSingleton<IAnsiConsole>(AnsiConsole.Console);

        // The theme is configuration (Tui:Theme), resolved by name. The host has already checked it before anything
        // can resolve this.
        services.AddSingleton<IMenuTheme>(sp => MenuThemes.ByName(
            sp.GetRequiredService<IConfiguration>()[MenuThemeKey]
            ?? throw new InvalidOperationException($"{MenuThemeKey} is not configured; the host checks it before the console starts.")));
        services.AddSingleton<SpectreRenderContext>(sp =>
            new SpectreRenderContext(sp.GetRequiredService<IAnsiConsole>(), sp.GetRequiredService<IMenuTheme>()));
        services.AddSingleton<SpectreUIRenderer>();
        services.AddSingleton<IRenderContext>(sp => sp.GetRequiredService<SpectreRenderContext>());
        services.AddSingleton<IUIRenderer>(sp => sp.GetRequiredService<SpectreUIRenderer>());

        services.AddTransient<MainMenuScreen>();
        services.AddTransient<ConnectionsScreen>();
        services.AddTransient<DashboardScreen>();
        services.AddTransient<ConfigurationMenuScreen>();
        services.AddTransient<MonitoringMenuScreen>();
        services.AddTransient<SettingsScreen>();

        services.AddSingleton<IConnectionManager, ConnectionManager>();
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<INewmanSuiteService, NewmanSuiteService>();

        // The connect-time reachability probe uses its own client: it must work before any instance is selected, so it
        // does not go through InstanceRoutingHandler.
        services.AddHttpClient(ConnectionManager.ProbeClientName, client => client.Timeout = TimeSpan.FromSeconds(10));
        services.AddBearerTokenHandler<InstanceAccessTokenProvider>();

        // Instance routing is TUI-specific: this is the only host that re-targets its clients at another instance at
        // runtime. Every API client type registers its named client in its own Register phase (category ApiClient,
        // collected before TuiManagement); this adds the routing handler to each.
        services.AddTransient<InstanceRoutingHandler>();
        foreach (var clientType in ApiClientTypes.All())
        {
            services.AddHttpClient(clientType.Value.Name).AddHttpMessageHandler<InstanceRoutingHandler>();
        }

        return GenericResult<IHostApplicationBuilder>.Success(builder);
    }

    /// <summary>Initialize phase: nothing to start.</summary>
    /// <param name="host">The built host.</param>
    /// <param name="loggerFactory">The logger factory.</param>
    /// <param name="force">Whether to run again.</param>
    /// <param name="defer">Whether the host runs this phase at a position it chooses.</param>
    /// <returns>The host.</returns>
    public static IGenericResult<IHost> Initialize(IHost host, ILoggerFactory? loggerFactory = null, bool force = false, bool defer = false)
        => GenericResult<IHost>.Success(host);
}
