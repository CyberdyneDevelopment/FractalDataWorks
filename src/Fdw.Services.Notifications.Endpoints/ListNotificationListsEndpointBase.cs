using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Notifications.Abstractions;
using Fdw.Services.Notifications.Endpoints.Logging;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Notifications.Endpoints;

/// <summary>
/// Base endpoint for listing all notification lists (recipient groups).
/// </summary>
public abstract class ListNotificationListsEndpointBase : CrudListEndpointBase<NotificationListSummaryDto>
{
    private readonly INotificationLists _lists;

    /// <summary>Initializes a new instance of the <see cref="ListNotificationListsEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="lists">Reads the notification lists from the configuration store.</param>
    protected ListNotificationListsEndpointBase(ILogger<ListNotificationListsEndpointBase> logger, INotificationLists lists) : base(logger)
    {
        _lists = lists;
    }

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

    // The lists the store holds; a store that holds none answers none, and one that cannot be read answers its failure.
    private async Task<IGenericResult<List<NotificationListSummaryDto>>> LoadNotificationLists(CancellationToken ct)
    {
        var lists = await _lists.List(ct).ConfigureAwait(false);
        return lists.IsSuccess && lists.Value is { } found
            ? GenericResult<List<NotificationListSummaryDto>>.Success([.. found])
            : lists.ToNewResult<List<NotificationListSummaryDto>>();
    }
}
