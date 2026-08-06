using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Notifications.Endpoints.Logging;
using Fdw.Web.RestEndpoints.Crud;

namespace Fdw.Services.Notifications.Endpoints;

/// <summary>
/// Base endpoint for listing all notification lists (recipient groups).
/// </summary>
public abstract class ListNotificationListsEndpointBase : CrudListEndpoint<NotificationListSummaryDto>
{
    /// <inheritdoc />
    protected override string ResourceName => "notifications/lists";

    /// <inheritdoc />
    protected override string EndpointSummary => "List notification lists";

    /// <inheritdoc />
    protected override string EndpointDescription => "Returns all notification recipient lists.";

    /// <inheritdoc />
    protected override Task<IGenericResult<List<NotificationListSummaryDto>>> LoadItems(CancellationToken ct)
    {
        NotificationEndpointLog.ListingNotificationLists(Logger);

        return LoadNotificationLists(ct);
    }

    /// <summary>
    /// Loads the notification lists. Override to implement data retrieval.
    /// </summary>
    protected abstract Task<IGenericResult<List<NotificationListSummaryDto>>> LoadNotificationLists(CancellationToken ct);
}
