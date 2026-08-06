#pragma warning disable CS1591
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Fdw.UI.Themes.Clients.Models;
using Fdw.UI.Providers;

namespace Fdw.UI.Themes.Components.Themes;

// Why: pure view-model — state + callback delegates only, no logic.
[ExcludeFromCodeCoverage]
public sealed class ThemeContext : ProviderContextBase
{
    // ── State ──────────────────────────────────────────────────────────────────

    public IReadOnlyList<ThemeSummaryPayload> Themes { get; init; } = [];
    public ThemeConfiguration? CurrentTheme { get; init; }

    // ── Callbacks ──────────────────────────────────────────────────────────────

    public Func<Task> OnLoadThemes { get; init; } = () => Task.CompletedTask;
    public Func<Task> OnLoadDefaultTheme { get; init; } = () => Task.CompletedTask;
    public Func<string, Task<bool>> OnSetDefaultTheme { get; init; } = _ => Task.FromResult(false);
    public Func<string, Task<bool>> OnDeleteTheme { get; init; } = _ => Task.FromResult(false);
    public Func<string, Task<ThemeConfiguration?>> OnGetTheme { get; init; } = _ => Task.FromResult<ThemeConfiguration?>(null);
    public Func<CreateThemeRequest, Task<ThemeConfiguration?>> OnCreateTheme { get; init; } = _ => Task.FromResult<ThemeConfiguration?>(null);
    public Func<string, UpdateThemeRequest, Task<ThemeConfiguration?>> OnUpdateTheme { get; init; } = (_, _) => Task.FromResult<ThemeConfiguration?>(null);
}
