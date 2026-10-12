using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Notifications.Abstractions;

/// <summary>
/// Interface for the notification dispatcher.
/// Routes notification requests to the appropriate channel-specific services.
/// </summary>
public interface INotificationDispatcher
{
    /// <summary>
    /// Sends a notification through the appropriate channel.
    /// </summary>
    /// <param name="request">The notification request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result of the send operation.</returns>
    Task<IGenericResult<INotificationResult>> Send(
        INotificationRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends multiple notifications, potentially to different channels.
    /// </summary>
    /// <param name="requests">The notification requests.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The results of the send operations.</returns>
    Task<IGenericResult<IEnumerable<INotificationResult>>> SendBatch(
        IEnumerable<INotificationRequest> requests,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets whether a specific channel is available.
    /// </summary>
    /// <param name="channelName">The name of the channel.</param>
    /// <returns>True if the channel is available.</returns>
    bool IsChannelAvailable(string channelName);

    /// <summary>
    /// Gets all available notification channels.
    /// </summary>
    /// <returns>The available channels.</returns>
    IEnumerable<INotificationChannel> GetAvailableChannels();
}
