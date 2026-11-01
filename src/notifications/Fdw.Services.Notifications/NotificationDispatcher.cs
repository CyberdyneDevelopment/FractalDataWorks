using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Notifications.Abstractions;
using Fdw.Services.Notifications.Logging;
using Fdw.Services.Notifications.Results;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Notifications;

/// <summary>
/// Dispatcher that routes notification requests to the appropriate channel services.
/// </summary>
public sealed class NotificationDispatcher : INotificationDispatcher
{
    private readonly IReadOnlyDictionary<string, INotificationService> _services;
    private readonly ILogger<NotificationDispatcher> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="NotificationDispatcher"/> class.
    /// </summary>
    /// <param name="services">The registered notification services.</param>
    /// <param name="logger">The logger instance.</param>
    public NotificationDispatcher(
        IEnumerable<INotificationService> services,
        ILogger<NotificationDispatcher> logger)
    {
        _logger = logger;
        _services = services.ToDictionary(
            s => s.Channel.Name,
            s => s,
            StringComparer.OrdinalIgnoreCase);
    }

    /// <inheritdoc/>
    public async Task<IGenericResult<INotificationResult>> Send(
        INotificationRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!_services.TryGetValue(request.ChannelName, out var service))
        {
            NotificationLogger.ChannelNotFound(_logger, request.ChannelName);
            return GenericResult<INotificationResult>.Failure(
                NotificationResultCodes.ByName("ChannelNotFound"),
                ResultDetails.Create().With("ChannelName", request.ChannelName));
        }

        var validationResult = service.Validate(request);
        if (!validationResult.IsSuccess)
        {
            var message = validationResult.CurrentMessage ?? "Validation failed";
            NotificationLogger.ValidationFailed(_logger, message);
            return GenericResult<INotificationResult>.Failure(
                NotificationResultCodes.ByName("ValidationFailed"),
                ResultDetails.Create().With("Message", message));
        }

        NotificationLogger.SendingNotification(_logger, request.ChannelName, request.Recipients.Count);

        var result = await service.Send(request, cancellationToken).ConfigureAwait(false);

        if (result.IsSuccess)
        {
            NotificationLogger.NotificationSent(_logger, request.RequestId, request.ChannelName);
        }
        else
        {
            NotificationLogger.NotificationFailed(_logger, request.RequestId, request.ChannelName, result.CurrentMessage ?? "Unknown error");
        }

        return result;
    }

    /// <inheritdoc/>
    public async Task<IGenericResult<IEnumerable<INotificationResult>>> SendBatch(
        IEnumerable<INotificationRequest> requests,
        CancellationToken cancellationToken = default)
    {
        var requestList = requests.ToList();
        NotificationLogger.SendingBatch(_logger, requestList.Count);

        var results = new List<INotificationResult>();
        var successCount = 0;
        var failCount = 0;

        foreach (var request in requestList)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            var result = await Send(request, cancellationToken).ConfigureAwait(false);
            if (result.IsSuccess && result.Value != null)
            {
                results.Add(result.Value);
                if (result.Value.IsSuccess)
                {
                    successCount++;
                }
                else
                {
                    failCount++;
                }
            }
            else
            {
                results.Add(NotificationResult.Failed(request.RequestId, result.CurrentMessage ?? "Unknown error"));
                failCount++;
            }
        }

        NotificationLogger.BatchComplete(_logger, successCount, failCount);

        return GenericResult<IEnumerable<INotificationResult>>.Success(results);
    }

    /// <inheritdoc/>
    public bool IsChannelAvailable(string channelName)
    {
        return _services.ContainsKey(channelName);
    }

    /// <inheritdoc/>
    public IEnumerable<INotificationChannel> GetAvailableChannels()
    {
        return _services.Values.Select(s => s.Channel);
    }
}
