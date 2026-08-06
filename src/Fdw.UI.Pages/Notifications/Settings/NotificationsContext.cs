using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fdw.Services.Notifications.Endpoints;
using Fdw.UI.Providers;

namespace Fdw.Services.Notifications.Components.Settings;

/// <summary>
/// Immutable context object passed to the consumer RenderFragment by <see cref="NotificationsProvider"/>.
/// Carries both state snapshots and callback delegates so that markup can stay free of logic.
/// </summary>
public sealed class NotificationsContext : ProviderContextBase
{
    // ── State ──────────────────────────────────────────────────────────────────

    /// <summary>Gets the list of notification configurations.</summary>
    public IReadOnlyList<NotificationSummaryDto> Notifications { get; init; } = [];

    /// <summary>Gets the list of notification rules.</summary>
    public IReadOnlyList<NotificationRuleSummaryDto> Rules { get; init; } = [];

    /// <summary>Gets the list of notification recipient lists.</summary>
    public IReadOnlyList<NotificationListSummaryDto> Lists { get; init; } = [];

    /// <summary>Gets the list of user notification preferences.</summary>
    public IReadOnlyList<UserNotificationPreferenceDto> Preferences { get; init; } = [];


    /// <summary>Gets whether a background refresh is in progress.</summary>
    public bool IsRefreshing { get; init; }


    // ── Callbacks ──────────────────────────────────────────────────────────────

    /// <summary>Invoked to reload notification configurations.</summary>
    public Func<Task> OnLoadNotifications { get; init; } = () => Task.CompletedTask;

    /// <summary>Invoked to reload notification rules.</summary>
    public Func<Task> OnLoadRules { get; init; } = () => Task.CompletedTask;

    /// <summary>Invoked to reload notification recipient lists.</summary>
    public Func<Task> OnLoadLists { get; init; } = () => Task.CompletedTask;

    /// <summary>Invoked to reload user notification preferences.</summary>
    public Func<Task> OnLoadPreferences { get; init; } = () => Task.CompletedTask;

    /// <summary>Invoked to save updated user notification preferences.</summary>
    public Func<IReadOnlyList<UserNotificationPreferenceDto>, Task> OnSavePreferences { get; init; } = _ => Task.CompletedTask;
}
