using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fdw.Services.Settings.Clients.Models;
using Fdw.UI.Themes.Clients.Models;
using Fdw.UI.Providers;

namespace Fdw.Services.Settings.Components.Settings;

/// <summary>
/// Immutable context object passed to the consumer RenderFragment by <see cref="SettingsProvider"/>.
/// Carries both state snapshots and callback delegates so that markup can stay free of logic.
/// </summary>
public sealed class SettingsContext : ProviderContextBase
{
    // -- State --

    /// <summary>Gets the list of server settings.</summary>
    public IReadOnlyList<ServerSettingResponse> Settings { get; init; } = [];

    /// <summary>Gets the list of available themes.</summary>
    public IReadOnlyList<ThemeSummaryPayload> Themes { get; init; } = [];


    /// <summary>Gets whether a save operation is in progress.</summary>
    public bool IsSaving { get; init; }


    /// <summary>Gets the most recent success message, or <c>null</c>.</summary>
    public string? SuccessMessage { get; init; }

    // -- Callbacks --

    /// <summary>Invoked to reload all settings from the API.</summary>
    public Func<Task> OnLoadSettings { get; init; } = () => Task.CompletedTask;

    /// <summary>Invoked to reload themes from the API.</summary>
    public Func<Task> OnLoadThemes { get; init; } = () => Task.CompletedTask;

    /// <summary>Invoked to update a single server setting by name.</summary>
    public Func<string, string?, Task> OnUpdateSetting { get; init; } = (_, _) => Task.CompletedTask;

    /// <summary>Invoked to set the default theme by name.</summary>
    public Func<string, Task> OnSetDefaultTheme { get; init; } = _ => Task.CompletedTask;

    /// <summary>Gets the value of a named setting, or the default if not found.</summary>
    public Func<string, string, string> GetSettingValue { get; init; } = (_, defaultValue) => defaultValue;
}
