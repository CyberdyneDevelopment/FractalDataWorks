using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Fdw.Results;
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
using System.Globalization;

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

            // The menu theme is configuration. Check it before anything resolves the theme, so a bad
            // name stops the console with the logged reason instead of rendering with NotFound.
            var menuTheme = ResolveMenuTheme(
                builder.Configuration[MenuThemeKey],
                host.Services.GetRequiredService<ILoggerFactory>().CreateLogger<ManagementApp>());
            if (menuTheme.IsFailure)
            {
                AnsiConsole.MarkupLineInterpolated(
                    CultureInfo.CurrentCulture,
                    $"[red]Startup failed:[/] {menuTheme.CurrentMessage}");
                return 1;
            }

            // Phase 3: eager initialize every registered domain.
            //
            var initialized = PlatformServices.Initialize(host);
            if (initialized.IsFailure)
            {
                AnsiConsole.MarkupLineInterpolated(
                    CultureInfo.CurrentCulture,
                    $"[red]Startup failed during Initialize:[/] {initialized.CurrentMessage}");
                return 1;
            }

            var app = host.Services.GetRequiredService<ManagementApp>();
            return await app.Run().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteException(ex);
            return 1;
        }
    }

    /// <summary>The configuration key that names the console's menu theme.</summary>
    private const string MenuThemeKey = "Tui:Theme";

    /// <summary>
    /// Resolves the configured menu theme by name.
    /// </summary>
    /// <param name="themeName">The configured theme name (<see cref="MenuThemeKey"/>).</param>
    /// <param name="logger">The logger the failure is written to.</param>
    /// <returns>The theme, or a failure carrying the logged message when the key is missing or names no theme.</returns>
    private static IGenericResult<IMenuTheme> ResolveMenuTheme(string? themeName, ILogger logger)
    {
        if (string.IsNullOrEmpty(themeName))
        {
            return GenericResult<IMenuTheme>.Failure(TuiStartupLog.MenuThemeNotConfigured(logger));
        }

        var theme = MenuThemes.ByName(themeName);
        if (ReferenceEquals(theme, MenuThemes.NotFound))
        {
            return GenericResult<IMenuTheme>.Failure(TuiStartupLog.MenuThemeNotFound(logger, themeName));
        }

        return GenericResult<IMenuTheme>.Success(theme);
    }

    private static void ConfigureApplication(HostApplicationBuilder builder)
    {
        builder.Logging.ClearProviders();
        // Only log warnings and errors in TUI mode to avoid cluttering the display
        builder.Logging.AddFilter("Fdw", LogLevel.Warning);

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            ["ApiClients:BaseUrl"] = PlaceholderApiBaseUrl,
            [MenuThemeKey] = "Dark",
        });

        var services = builder.Services;

        // Register the management app
        services.AddSingleton<ManagementApp>();

        // Register navigation
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<IScreenFactory, ScreenFactory>();

        // Register Spectre console
        services.AddSingleton<IAnsiConsole>(AnsiConsole.Console);

        // The theme is configuration (Tui:Theme), resolved by name. Main has already checked it
        // (ResolveMenuTheme) before anything can resolve this.
        services.AddSingleton<IMenuTheme>(sp => MenuThemes.ByName(sp.GetRequiredService<IConfiguration>()[MenuThemeKey]!));

        // Register render context
        services.AddSingleton<SpectreRenderContext>(sp =>
        {
            var console = sp.GetRequiredService<IAnsiConsole>();
            var theme = sp.GetRequiredService<IMenuTheme>();
            return new SpectreRenderContext(console, theme);
        });

        // Register UI renderer
        services.AddSingleton<SpectreUIRenderer>();

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

        services.AddSingleton<INewmanSuiteService, NewmanSuiteService>();

        ConfigureApiClients(builder);
    }

    /// <summary>
    /// Wires the shared Fdw API clients so the TUI talks to a real instance.
    /// </summary>
    /// <remarks>
    /// The clients themselves already exist — every <c>.Clients</c> package registers its own
    /// <c>[Implementation(typeof(ApiClientTypes), ...)]</c> with a named HttpClient and the shared
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

        services.AddBearerTokenHandler<InstanceAccessTokenProvider>();

        // Instance routing has no shared extension — it is genuinely TUI-specific, because this is
        // the only host that re-targets its clients at a different instance at runtime.
        services.AddTransient<InstanceRoutingHandler>();

        // Phases 1 and 2: let every discovered domain (ApiClientTypes among them) configure and
        // register itself. A failure here is thrown as an InvalidOperationException rather than
        // returned, because this method returns void to its caller and the host cannot continue with a
        // half-registered container — Main's catch turns it into the same operator-facing message.
        var configured = PlatformServices.Configure(builder);
        if (configured.IsFailure)
            throw new InvalidOperationException($"Configure phase failed: {configured.CurrentMessage}");

        var registered = PlatformServices.Register(builder);
        if (registered.IsFailure)
            throw new InvalidOperationException($"Register phase failed: {registered.CurrentMessage}");

        foreach (var clientType in ApiClientTypes.All())
        {
            services.AddHttpClient(clientType.Value.Name).AddHttpMessageHandler<InstanceRoutingHandler>();
        }
    }
}
