using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fdw.UI.Providers;
// Why: Fdw.Services.Messaging.MessagePayload (server-side) is accessible via
// ancestor-namespace lookup from this namespace (Services.Messaging.Components.Bell).
// Namespace alias bypasses ancestor-namespace lookup so ClientModels.MessagePayload is unambiguous.
using ClientModels = Fdw.Services.Messaging.Clients.Models;

namespace Fdw.Services.Messaging.Components.Bell;

/// <summary>
/// Immutable context object passed to the consumer RenderFragment by <see cref="NotificationBellProvider"/>.
/// Provides unread count and recent messages for a notification bell UI element.
/// </summary>
public sealed class NotificationBellContext : ProviderContextBase
{
    // -- State --

    /// <summary>Gets the number of unread messages.</summary>
    public int UnreadCount { get; init; }

    /// <summary>Gets the most recent messages for the dropdown preview.</summary>
    public IReadOnlyList<ClientModels.MessagePayload> RecentMessages { get; init; } = [];



    // -- Callbacks --


    /// <summary>Invoked to mark a specific message as read.</summary>
    public Func<Guid, Task> OnMarkRead { get; init; } = _ => Task.CompletedTask;

    /// <summary>Invoked to mark all messages as read.</summary>
    public Func<Task> OnMarkAllRead { get; init; } = () => Task.CompletedTask;
}
