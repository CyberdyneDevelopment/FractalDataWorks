using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fdw.Operations.Clients;
using Fdw.UI.Providers;

namespace Fdw.Services.Notifications.Components.Settings;

/// <summary>
/// Immutable context object passed to the consumer RenderFragment by <see cref="NotificationSettingsProvider"/>.
/// Carries both state snapshots and callback delegates so that markup can stay free of logic.
/// </summary>
public sealed class NotificationSettingsContext : ProviderContextBase
{
    // ── State ──────────────────────────────────────────────────────────────────

    /// <summary>Gets the list of notification preferences for the current user.</summary>
    public IReadOnlyList<NotificationPreferencePayload> Preferences { get; init; } = [];


    /// <summary>Gets whether preferences are being saved.</summary>
    public bool IsSaving { get; init; }


    // ── Callbacks ──────────────────────────────────────────────────────────────

    /// <summary>Invoked to reload notification preferences.</summary>
    public Func<Task> OnLoadPreferences { get; init; } = () => Task.CompletedTask;

    /// <summary>Invoked to save updated notification preferences.</summary>
    public Func<IReadOnlyList<NotificationPreferencePayload>, Task> OnSavePreferences { get; init; } = _ => Task.CompletedTask;
}
