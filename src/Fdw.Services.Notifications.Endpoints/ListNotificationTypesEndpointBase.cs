using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Notifications.Endpoints;

/// <summary>
/// Base endpoint serving the notification types the application has with the settings each needs: <c>GET /notifications/types</c> (FDW-2931).
/// </summary>
public abstract class ListNotificationTypesEndpointBase : CrudListEndpointBase<NotificationTypeDto>
{
    /// <summary>Initializes a new instance of the <see cref="ListNotificationTypesEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    protected ListNotificationTypesEndpointBase(ILogger<ListNotificationTypesEndpointBase> logger) : base(logger)
    {
    }

    /// <inheritdoc />
    protected override string ResourceName => "notifications/types";

    /// <inheritdoc />
    protected override string ReadPolicy => "notifications:read";

    /// <inheritdoc />
    protected override string EndpointSummary => "List the notification types and the settings each needs";

    /// <inheritdoc />
    protected override string EndpointDescription =>
        "Returns each notification type the application has with the settings a notification of it needs. A secret setting is named with secretManagerName and secretKeyName, never sent as a value.";

    /// <inheritdoc />
    protected override Task<IGenericResult<List<NotificationTypeDto>>> LoadItems(CancellationToken ct)
        => Task.FromResult(GenericResult<List<NotificationTypeDto>>.Success(
            [.. NotificationServiceTypes.All().Select(pair => new NotificationTypeDto
            {
                Name = pair.Value.Name, DisplayName = pair.Value.DisplayName, Description = pair.Value.Description, Settings = pair.Value.Settings,
            })]));
}
