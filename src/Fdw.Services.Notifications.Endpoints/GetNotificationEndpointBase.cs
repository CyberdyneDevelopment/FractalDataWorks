using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Notifications.Abstractions;
using Fdw.Services.Notifications.Endpoints.Logging;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Notifications.Endpoints;

/// <summary>
/// Base endpoint for getting a notification configuration by name.
/// </summary>
public abstract class GetNotificationEndpointBase : CrudGetEndpointBase<NotificationNameRequest, NotificationDetailDto>
{
    private readonly INotificationConfigurationProvider _provider;

    /// <inheritdoc />
    protected GetNotificationEndpointBase(ILogger<GetNotificationEndpointBase> logger, INotificationConfigurationProvider provider) : base(logger)
    {
        _provider = provider;
    }

    /// <inheritdoc />
    protected override string ResourceName => "notifications";

    /// <inheritdoc />
    protected override string Route => "/notifications/{NotificationName}";

    /// <inheritdoc />
    protected override string EndpointSummary => "Get notification by name";

    /// <inheritdoc />
    protected override string GetResourceIdentifier(NotificationNameRequest request) => request.NotificationName;

    /// <inheritdoc />
    protected override async Task<IGenericResult<NotificationDetailDto?>> FindByIdentifier(NotificationNameRequest request, CancellationToken ct)
    {
        NotificationEndpointLog.GettingNotification(Logger, request.NotificationName);

        var notificationResult = await _provider.Get(request.NotificationName, ct).ConfigureAwait(false);

        if (!notificationResult.IsSuccess || notificationResult.Value is null)
        {
            NotificationEndpointLog.NotificationNotFound(Logger, request.NotificationName);
            return GenericResult<NotificationDetailDto?>.Success(null);
        }

        var notification = notificationResult.Value;
        var detail = new NotificationDetailDto
        {
            Id = notification.Id,
            Name = notification.Name,
            Implementation = notification.Implementation,
        };

        return GenericResult<NotificationDetailDto?>.Success(detail);
    }
}
