using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Notifications.Endpoints.Logging;
using Fdw.Web.RestEndpoints.Crud;

namespace Fdw.Services.Notifications.Endpoints;

/// <summary>
/// Base endpoint for getting a notification configuration by name.
/// </summary>
public abstract class GetNotificationEndpointBase : CrudGetEndpoint<NotificationNameRequest, NotificationDetailDto>
{
    // Why: NotificationConfigurationProvider replaces IOptionsMonitor<List<NotificationConfiguration>>
    // with dual-source (ctrl + cfg) provider that merges system and user configurations.
    private readonly NotificationConfigurationProvider _provider;

    /// <inheritdoc />
    protected GetNotificationEndpointBase(NotificationConfigurationProvider provider)
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
        // Why: GetNotification reads the parent header (notify.Notification) which is identity-only
        // per the polymorphic configuration pattern. Runtime fields (IsEnabled, SecretManagerName,
        // SecretKeyName) live on typed-body tables and are not accessible from the parent query path.
        // Typed endpoints supply those fields when a typed-body provider is bound.
        var detail = new NotificationDetailDto
        {
            Id = notification.Id,
            Name = notification.Name,
            ServiceOptionType = notification.ServiceOptionType,
            Description = notification.Description
        };

        return GenericResult<NotificationDetailDto?>.Success(detail);
    }
}
