using System;
using Fdw.UI.Abstractions.RenderModeOptions;
using Fdw.UI.Abstractions.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Fdw.UI.Rendering.Blazor.Extensions;

/// <summary>
/// DI registration helpers for the Blazor rendering backend. Hosting layers call
/// <c>AddFrameworkBlazorUI</c> to wire the renderer and its supporting services.
/// </summary>
// Why: mirrors SpectreRegistrationExtensions — each rendering backend self-describes its
// registration so hosts pick a backend by calling exactly one AddFramework*UI method.
public static class BlazorRegistrationExtensions
{
    /// <summary>
    /// Registers <see cref="BlazorUIRenderer"/> as <see cref="IUIRenderer"/> and a
    /// <see cref="BlazorRenderContext"/> factory. Idempotent.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="defaultRenderModeName">
    /// Optional override for the default render mode name. Defaults to Edit.
    /// </param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddFrameworkBlazorUI(
        this IServiceCollection services,
        string defaultRenderModeName = "Edit")
    {
        if (services is null) throw new ArgumentNullException(nameof(services));

        // Why: transient so each interaction flow gets its own fragment sink; contexts are
        // stateful per render conversation, not per app.
        services.TryAddTransient<BlazorRenderContext>(_ =>
            new BlazorRenderContext(RenderModes.ByName(defaultRenderModeName)));

        services.TryAddSingleton<BlazorUIRenderer>();
        services.TryAddSingleton<IUIRenderer>(sp => sp.GetRequiredService<BlazorUIRenderer>());

        return services;
    }
}
