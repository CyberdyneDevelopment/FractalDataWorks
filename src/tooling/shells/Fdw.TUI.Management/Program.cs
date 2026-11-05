using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.ServiceTypes;
using Fdw.UI.Themes;
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
    /// only exists to make those named clients register — <see cref="Services.Api.InstanceRoutingHandler"/> rewrites
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
    private const string MenuThemeKey = ManagementAppHosting.MenuThemeKey;

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

        // Phases 1 and 2: every discovered domain configures and registers itself, the console's own services
        // included (ManagementAppHosting, FDW-3957); the application registers nothing (FDW056). A failure is thrown as
        // an InvalidOperationException because this method returns void and the host cannot continue with a
        // half-registered container; Main's catch turns it into the same operator-facing message.
        var configured = PlatformServices.Configure(builder);
        if (configured.IsFailure)
            throw new InvalidOperationException($"Configure phase failed: {configured.CurrentMessage}");

        var registered = PlatformServices.Register(builder);
        if (registered.IsFailure)
            throw new InvalidOperationException($"Register phase failed: {registered.CurrentMessage}");
    }
}
