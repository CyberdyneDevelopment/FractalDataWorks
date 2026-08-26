using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Notifications.Abstractions;

/// <summary>
/// Interface for channel-specific notification services.
/// Each notification channel (Email, Teams, Webhook) has its own implementation.
/// Extends IPlatformNotification with notification-specific operations.
/// </summary>
public interface INotificationService : IPlatformNotification
{
    /// <summary>
    /// Sends a notification through this channel.
    /// </summary>
    /// <param name="request">The notification request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result of the send operation.</returns>
    Task<IGenericResult<INotificationResult>> Send(
        INotificationRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates whether a notification request is valid for this channel.
    /// </summary>
    /// <param name="request">The notification request to validate.</param>
    /// <returns>A result indicating whether the request is valid.</returns>
    IGenericResult Validate(INotificationRequest request);
}
