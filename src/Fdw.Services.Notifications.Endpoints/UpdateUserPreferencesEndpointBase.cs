using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Notifications.Endpoints.Logging;
using Fdw.Web.RestEndpoints.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Notifications.Endpoints;

/// <summary>
/// Base endpoint for updating user notification preferences.
/// </summary>
public abstract class UpdateUserPreferencesEndpointBase : Endpoint<UpdateUserPreferencesRequest, IReadOnlyList<UserNotificationPreferenceDto>>
{
    /// <summary>Initializes a new instance of the <see cref="UpdateUserPreferencesEndpointBase"/> class.</summary>
    protected UpdateUserPreferencesEndpointBase(ILogger<UpdateUserPreferencesEndpointBase> logger)
    {
        Logger = logger;
    }

    /// <summary>
    /// Gets the logger instance.
    /// </summary>
    protected new ILogger Logger { get; }

    /// <inheritdoc/>
    public override void Configure()
    {
        Patch("/users/{UserId}/notification-preferences");
#if DEVELOP
        AllowAnonymous();
#else
        Policies("notifications:write");
#endif
        Summary(s =>
        {
            s.Summary = "Update user notification preferences";
            s.Description = "Updates notification preferences for a user.";
        });
    }

    /// <inheritdoc/>
    public override async Task HandleAsync(UpdateUserPreferencesRequest req, CancellationToken ct)
    {
        try
        {
            var userId = req.UserId.ToString("D", CultureInfo.InvariantCulture);

            var saved = await SavePreferences(req.UserId, req.Preferences, ct).ConfigureAwait(false);

            NotificationEndpointLog.UpdatedUserPreferences(Logger, userId);

            await Send.OkAsync(saved, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            EndpointLogger.EndpointError(Logger, ex, GetType().Name);
            HttpContext.Response.StatusCode = 500;
        }
    }

    /// <summary>
    /// Persists the user preferences. Override to implement storage.
    /// </summary>
    protected abstract Task<IReadOnlyList<UserNotificationPreferenceDto>> SavePreferences(
        Guid userId,
        IReadOnlyList<UserNotificationPreferenceDto> preferences,
        CancellationToken ct);
}
