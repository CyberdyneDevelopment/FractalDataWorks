namespace Fdw.Operations.Clients;

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Web.Clients.Abstractions;
using Microsoft.Extensions.Logging;

/// <summary>
/// API client for user notification preferences endpoints.
/// </summary>
public class NotificationPreferencesApiClient : ApiClientBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NotificationPreferencesApiClient"/> class.
    /// </summary>
    public NotificationPreferencesApiClient(HttpClient httpClient, ILogger<NotificationPreferencesApiClient> logger)
        : base(httpClient, logger)
    {
    }

    /// <summary>
    /// Gets notification preferences for a user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the list of notification preferences.</returns>
    public virtual Task<IGenericResult<IReadOnlyList<NotificationPreferencePayload>>> GetPreferences(
        Guid userId,
        CancellationToken ct = default)
        => GetList<NotificationPreferencePayload>($"users/{userId:D}/notification-preferences", ct);

    /// <summary>
    /// Updates notification preferences for a user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="preferences">The preferences to save.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the updated list of notification preferences.</returns>
    public virtual Task<IGenericResult<IReadOnlyList<NotificationPreferencePayload>>> UpdatePreferences(
        Guid userId,
        IReadOnlyList<NotificationPreferencePayload> preferences,
        CancellationToken ct = default)
    {
        var request = new UpdateNotificationPreferencesRequest
        {
            UserId = userId,
            Preferences = preferences
        };
        return Put<UpdateNotificationPreferencesRequest, IReadOnlyList<NotificationPreferencePayload>>(
            $"users/{userId:D}/notification-preferences", request, ct);
    }
}
