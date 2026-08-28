using System;
using Fdw.UI.Abstractions.Rendering;
using Fdw.UI.Themes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Spectre.Console;

namespace Fdw.UI.Rendering.Spectre.Extensions;

/// <summary>
/// DI registration helpers for the Spectre.Console rendering backend. Hosting layers
/// (web app, CLI, tools) call <c>AddFrameworkSpectreUI</c> to wire the renderer and
/// its supporting services.
/// </summary>
public static class SpectreRegistrationExtensions
{
    /// <summary>
    /// Registers <see cref="SpectreUIRenderer"/> as <see cref="IUIRenderer"/>, the
    /// process-wide <see cref="IAnsiConsole"/>, and a <see cref="SpectreRenderContext"/>
    /// factory. Idempotent.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="defaultThemeId">
    /// Optional override for the default theme id. Defaults to dark theme (id 1).
    /// </param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddFrameworkSpectreUI(
        this IServiceCollection services,
        int defaultThemeId = 1)
    {
        if (services is null) throw new ArgumentNullException(nameof(services));

        services.TryAddSingleton<IAnsiConsole>(_ => AnsiConsole.Console);

        services.TryAddTransient<SpectreRenderContext>(sp =>
        {
            var console = sp.GetRequiredService<IAnsiConsole>();
            var theme = MenuThemes.ById(defaultThemeId);
            return new SpectreRenderContext(console, theme);
        });

        services.TryAddSingleton<SpectreUIRenderer>();
        services.TryAddSingleton<IUIRenderer>(sp => sp.GetRequiredService<SpectreUIRenderer>());

        return services;
    }
}
