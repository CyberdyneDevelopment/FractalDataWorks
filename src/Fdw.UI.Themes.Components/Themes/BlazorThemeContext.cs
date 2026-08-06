#pragma warning disable CS1591
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Fdw.UI.Themes;
using Fdw.UI.Providers;

namespace Fdw.UI.Themes.Components.Themes;

/// <summary>
/// Immutable context for the Blazor theme provider.
/// Cascaded to child components so they can read the current theme
/// and request theme switches.
/// </summary>
// Why: pure view-model — state + callback delegates only, no logic.
[ExcludeFromCodeCoverage]
public sealed class BlazorThemeContext : ProviderContextBase
{
    // ── State ──────────────────────────────────────────────────────────────────

    /// <summary>Gets the currently active Blazor theme.</summary>
    public IBlazorTheme CurrentTheme { get; init; } = default!;

    /// <summary>Gets the names of all registered themes.</summary>
    public IReadOnlyList<string> AvailableThemes { get; init; } = [];

    // ── Callbacks ──────────────────────────────────────────────────────────────

    /// <summary>Requests switching to the theme with the specified name.</summary>
    public Func<string, Task> OnSwitchTheme { get; init; } = _ => Task.CompletedTask;
}
