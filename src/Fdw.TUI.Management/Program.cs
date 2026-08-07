using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Fdw.ServiceTypes;
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
/// Entry point for the Fdw TUI Management application.
/// </summary>
[ExcludeFromCodeCoverage]
public static class Program
{
    /// <summary>
    /// Why a placeholder: the shared API-client options bake a BaseAddress into their named HttpClient
    /// at registration time from ApiClients:BaseUrl. The TUI picks its instance at runtime, so this value
    /// only exists to make those named clients register — <see cref="InstanceRoutingHandler"/> rewrites
    /// every request onto the connected instance, and refuses the call outright when none is connected.
    /// </summary>
    private const string PlaceholderApiBaseUrl = "http://instance.invalid/";

    /// <summary>
    /// Main entry point.
    /// </summary>
    public static async Task<int> Main(string[] args)
    {
        try
        {
            var builder = Host.CreateApplicationBuilder(args);
            ConfigureApplication(builder);

            var host = builder.Build();

            // Phase 3: eager initialize every registered domain (fail fast).
            PlatformServices.Initialize(host);

            var app = host.Services.GetRequiredService<ManagementApp>();
            return await app.Run().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteException(ex);
            return 1;
        }
    }

    private static void ConfigureApplication(HostApplicationBuilder builder)
    {
        builder.Logging.ClearProviders();
        // Only log warnings and errors in TUI mode to avoid cluttering the display
        builder.Logging.AddFilter("Fdw", LogLevel.Warning);

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            ["ApiClients:BaseUrl"] = PlaceholderApiBaseUrl,
        });

        var services = builder.Services;

        // Register the management app
        services.AddSingleton<ManagementApp>();

        // Register navigation
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<IScreenFactory, ScreenFactory>();

        // Register Spectre console
        services.AddSingleton<IAnsiConsole>(AnsiConsole.Console);

        // Register theme
        services.AddSingleton<IMenuTheme>(sp =>
        {
            // Use the Dark theme from MenuThemes collection
            return MenuThemes.ById(1) ?? new DarkMenuTheme();
        });

        // Register render context
        services.AddSingleton<SpectreRenderContext>(sp =>
        {
            var console = sp.GetRequiredService<IAnsiConsole>();
            var theme = sp.GetRequiredService<IMenuTheme>();
            return new SpectreRenderContext(console, theme);
        });

        // Register UI renderer
        services.AddSingleton<SpectreUIRenderer>();

        // Why: this composition root is the ONE place that names a rendering backend. Screens
        // depend only on the render-agnostic seam (IUIRenderer + IRenderContext), so retargeting
        // this app at another registered UIRenderers option means changing these two lines —
        // not the screens.
        services.AddSingleton<IRenderContext>(sp => sp.GetRequiredService<SpectreRenderContext>());
        services.AddSingleton<IUIRenderer>(sp => sp.GetRequiredService<SpectreUIRenderer>());

        // Register screens
        services.AddTransient<MainMenuScreen>();
        services.AddTransient<ConnectionsScreen>();
        services.AddTransient<DashboardScreen>();
        services.AddTransient<ConfigurationMenuScreen>();
        services.AddTransient<MonitoringMenuScreen>();
        services.AddTransient<SettingsScreen>();

        // Register services
        services.AddSingleton<IConnectionManager, ConnectionManager>();
        services.AddSingleton<ISettingsService, SettingsService>();

        ConfigureApiClients(builder);
    }

    /// <summary>
    /// Wires the shared Fdw API clients so the TUI talks to a real instance.
    /// </summary>
    /// <remarks>
    /// The clients themselves already exist — every <c>.Clients</c> package registers its own
    /// <c>[ServiceTypeOption(typeof(ApiClientTypes), ...)]</c> with a named HttpClient and the shared
    /// bearer-token handler. All this app supplies is the two seams that make them instance-aware:
    /// the credential (<see cref="InstanceAccessTokenProvider"/>) and the address
    /// (<see cref="InstanceRoutingHandler"/>).
    /// </remarks>
    private static void ConfigureApiClients(HostApplicationBuilder builder)
    {
        var services = builder.Services;

        // The connect-time reachability probe uses its own client — it must work before any instance
        // is selected, so it deliberately does not go through InstanceRoutingHandler.
        services.AddHttpClient(ConnectionManager.ProbeClientName, client =>
        {
            client.Timeout = TimeSpan.FromSeconds(10);
        });

        // Why this one call: the shared extension registers BOTH the token provider and the
        // BearerTokenHandler itself. The client options only ATTACH the handler to their pipeline
        // (the IHttpClientBuilder overload) — putting it in DI is the app's job, and this is the
        // seam Fdw provides for it. The named HttpClients themselves need nothing from us: each
        // client option's Configure already calls AddHttpClient(Name, ...) during the sweep below.
        services.AddBearerTokenHandler<InstanceAccessTokenProvider>();

        // Instance routing has no shared extension — it is genuinely TUI-specific, because this is
        // the only host that re-targets its clients at a different instance at runtime.
        services.AddTransient<InstanceRoutingHandler>();

        // Phase 1: let every discovered domain (ApiClientTypes among them) configure and register itself.
        PlatformServices.Configure(builder);
        PlatformServices.Register(builder);

        // Why iterate the collection instead of naming clients: a newly referenced .Clients package
        // registers itself as an ApiClientTypes option, and picks up instance routing automatically.
        foreach (var clientType in ApiClientTypes.All())
        {
            services.AddHttpClient(clientType.Value.Name).AddHttpMessageHandler<InstanceRoutingHandler>();
        }
    }
}
