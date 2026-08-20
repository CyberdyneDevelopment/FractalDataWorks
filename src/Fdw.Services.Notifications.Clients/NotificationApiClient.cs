namespace Fdw.Services.Notifications.Clients;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Notifications.Clients.Models;
using Fdw.Services.Notifications.Endpoints;
using Fdw.Web.Clients.Abstractions;
using Microsoft.Extensions.Logging;

/// <summary>
/// API client for notification management — domain-level lists (notifications, rules, recipient
/// lists) and per-user notification preferences.
/// </summary>
public class NotificationApiClient : ApiClientBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NotificationApiClient"/> class.
    /// </summary>
    public NotificationApiClient(HttpClient httpClient, ILogger<NotificationApiClient> logger)
        : base(httpClient, logger)
    {
    }

    /// <summary>
    /// Gets the list of all notification configurations.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the list of notification summaries.</returns>
    public virtual Task<IGenericResult<IReadOnlyList<NotificationSummaryDto>>> ListNotifications(
        CancellationToken ct = default)
        => GetList<NotificationSummaryDto>("notifications", ct);

    /// <summary>
    /// Gets the list of all notification rules.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the list of notification rule summaries.</returns>
    public virtual Task<IGenericResult<IReadOnlyList<NotificationRuleSummaryDto>>> ListRules(
        CancellationToken ct = default)
        => GetList<NotificationRuleSummaryDto>("notifications/rules", ct);

    /// <summary>
    /// Gets the list of all notification recipient lists.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the list of recipient-list summaries.</returns>
    /// <remarks>
    /// Callers beware: the server endpoint backing this route is currently a stub that always
    /// returns an empty list (Reference.Api's ListNotificationListsEndpoint has a TODO to load from
    /// cfg.NotificationList once ManagedConfiguration is wired). An empty result from this method is
    /// therefore NOT evidence that no recipient lists are configured, and must not be presented to a
    /// user as though it were.
    /// </remarks>
    public virtual Task<IGenericResult<IReadOnlyList<NotificationListSummaryDto>>> ListNotificationLists(
        CancellationToken ct = default)
        => GetList<NotificationListSummaryDto>("notifications/lists", ct);

    /// <summary>
    /// Gets the notification preferences for a specific user.
    /// </summary>
    /// <returns>A result containing the list of notification preferences.</returns>
    public virtual Task<IGenericResult<IReadOnlyList<UserNotificationPreferenceResponse>>> GetPreferences(
        Guid userId, CancellationToken ct = default)
        => GetList<UserNotificationPreferenceResponse>(
            string.Format(CultureInfo.InvariantCulture, "users/{0}/notification-preferences",
                userId.ToString("D", CultureInfo.InvariantCulture)), ct);

    /// <summary>
    /// Saves the notification preferences for a specific user.
    /// </summary>
    /// <returns>A result containing the updated list of notification preferences.</returns>
    public virtual Task<IGenericResult<IReadOnlyList<UserNotificationPreferenceResponse>>> SavePreferences(
        Guid userId, UpdateUserPreferencesPayload request, CancellationToken ct = default)
        => Patch<UpdateUserPreferencesPayload, IReadOnlyList<UserNotificationPreferenceResponse>>(
            string.Format(CultureInfo.InvariantCulture, "users/{0}/notification-preferences",
                userId.ToString("D", CultureInfo.InvariantCulture)), request, ct);
}
