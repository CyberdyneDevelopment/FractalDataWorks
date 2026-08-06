using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Notifications.Abstractions;

/// <summary>
/// Reads and writes per-user notification preferences (the notification-type /
/// delivery-channel enabled toggles) persisted in <c>notify.UserNotificationPreference</c>.
/// </summary>
public interface IUserNotificationPreferenceService
{
    /// <summary>
    /// Loads the persisted notification preferences for a user.
    /// </summary>
    /// <param name="userId">The user whose preferences to load.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The user's persisted preferences (empty when none are stored).</returns>
    Task<IGenericResult<IReadOnlyList<NotificationPreference>>> GetPreferences(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Replaces the full set of notification preferences for a user with the supplied set
    /// (each notification-type / channel pair is upserted; the supplied set is authoritative).
    /// </summary>
    /// <param name="userId">The user whose preferences to save.</param>
    /// <param name="preferences">The preferences to persist.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The persisted preferences read back from storage.</returns>
    Task<IGenericResult<IReadOnlyList<NotificationPreference>>> SavePreferences(
        Guid userId,
        IReadOnlyList<NotificationPreference> preferences,
        CancellationToken cancellationToken = default);
}
