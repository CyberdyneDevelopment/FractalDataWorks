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
/// Base endpoint for getting user notification preferences.
/// Returns default preferences when no persisted preferences exist.
/// </summary>
public abstract class ListUserPreferencesEndpointBase : Endpoint<UserPreferencesRequest, IReadOnlyList<UserNotificationPreferenceDto>>
{
    /// <summary>
    /// Gets the logger instance.
    /// </summary>
    protected new ILogger Logger { get; private set; } = null!;

    /// <inheritdoc/>
    public override void Configure()
    {
        Get("/users/{UserId}/notification-preferences");
#if DEVELOP
        AllowAnonymous();
#else
        Policies("notifications:read");
#endif
        Summary(s =>
        {
            s.Summary = "Get user notification preferences";
            s.Description = "Returns notification preferences for a user. Returns defaults if none are configured.";
        });
    }

    /// <inheritdoc/>
    public override async Task HandleAsync(UserPreferencesRequest req, CancellationToken ct)
    {
        Logger = Resolve<ILoggerFactory>().CreateLogger(GetType());

        try
        {
            var userId = req.UserId.ToString("D", CultureInfo.InvariantCulture);
            NotificationEndpointLog.ListingUserPreferences(Logger, userId);

            var preferences = await LoadPreferences(req.UserId, ct).ConfigureAwait(false);

            if (preferences.Count == 0)
            {
                NotificationEndpointLog.UserPreferencesNotFound(Logger, userId);
                preferences = GetDefaultPreferences();
            }

            await Send.OkAsync(preferences, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            EndpointLogger.EndpointError(Logger, ex, GetType().Name);
            HttpContext.Response.StatusCode = 500;
        }
    }

    /// <summary>
    /// Loads persisted preferences for the user. Override to implement data retrieval.
    /// </summary>
    protected abstract Task<IReadOnlyList<UserNotificationPreferenceDto>> LoadPreferences(Guid userId, CancellationToken ct);

    /// <summary>
    /// Returns the default set of notification preferences.
    /// Override to customize defaults.
    /// </summary>
    protected virtual IReadOnlyList<UserNotificationPreferenceDto> GetDefaultPreferences()
    {
        return
        [
            new() { NotificationType = "PipelineFailure", Channel = "InApp", IsEnabled = true },
            new() { NotificationType = "PipelineFailure", Channel = "Email", IsEnabled = true },
            new() { NotificationType = "PipelineCompleted", Channel = "InApp", IsEnabled = true },
            new() { NotificationType = "PipelineCompleted", Channel = "Email", IsEnabled = false },
            new() { NotificationType = "ScheduleTrigger", Channel = "InApp", IsEnabled = true },
            new() { NotificationType = "ScheduleTrigger", Channel = "Email", IsEnabled = false },
            new() { NotificationType = "ConnectionIssue", Channel = "InApp", IsEnabled = true },
            new() { NotificationType = "ConnectionIssue", Channel = "Email", IsEnabled = false },
            new() { NotificationType = "SystemUpdate", Channel = "InApp", IsEnabled = true },
            new() { NotificationType = "SystemUpdate", Channel = "Email", IsEnabled = false },
            new() { NotificationType = "AccessRequest", Channel = "InApp", IsEnabled = true },
            new() { NotificationType = "AccessRequest", Channel = "Email", IsEnabled = true },
            new() { NotificationType = "MessageReceived", Channel = "InApp", IsEnabled = true },
            new() { NotificationType = "MessageReceived", Channel = "Email", IsEnabled = false },
        ];
    }
}
