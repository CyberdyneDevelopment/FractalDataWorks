using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Configuration;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Notifications.Configuration;
using Fdw.Services.Notifications.Endpoints.Logging;
using Fdw.Web.RestEndpoints.Crud;
using Fdw.Web.RestEndpoints.Models;
using Microsoft.Extensions.Logging;
using Fdw.Services.Notifications.Abstractions;

namespace Fdw.Services.Notifications.Endpoints;

/// <summary>
/// Base endpoint for listing notification configurations, one page at a time.
/// </summary>
/// <remarks>
/// A page is one read of the notifications domain table: a notification's summary is its id, its name and its
/// implementation, all on the domain row, so no notification is composed to list it (FDW-1005). It streams the
/// page envelope <c>{skip, take, hasMore, items}</c> every domain list uses; it used to load every notification
/// whole and answer an in-memory <c>PaginatedResponse</c> (items and totalCount).
/// </remarks>
public abstract class ListNotificationsEndpointBase : CrudPageEndpointBase<NotificationDomainConfiguration, NotificationSummaryDto>
{
    private readonly NotificationConfigurationProvider _provider;

    /// <inheritdoc />
    protected ListNotificationsEndpointBase(ILogger<ListNotificationsEndpointBase> logger, NotificationConfigurationProvider provider) : base(logger)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
    }

    /// <inheritdoc />
    protected override string ResourceName => "notifications";

    /// <inheritdoc />
    protected override string EndpointSummary => "List notifications";

    /// <inheritdoc />
    protected override string EndpointDescription => "Streams one page of notification configurations in name order.";

    /// <inheritdoc />
    protected override Task<IGenericResult<ConfigurationPage<NotificationDomainConfiguration>>> LoadPage(
        PaginatedListRequest paging, CancellationToken ct)
    {
        NotificationEndpointLog.ListingNotifications(Logger);
        return _provider.Get(new DomainQuery(null, null, paging.Skip, paging.Take, false), ct);
    }

    /// <inheritdoc />
    protected override Task<IGenericResult<NotificationSummaryDto>> ToSummary(NotificationDomainConfiguration item, CancellationToken ct)
        => Task.FromResult(GenericResult<NotificationSummaryDto>.Success(NotificationMapper.ToSummary(item)));
}
