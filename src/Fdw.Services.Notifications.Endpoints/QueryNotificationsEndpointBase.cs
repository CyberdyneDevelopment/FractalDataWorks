using Fdw.Services.Notifications.Configuration;
using Fdw.Web.RestEndpoints.Query;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Notifications.Endpoints;

/// <summary>
/// Base endpoint for querying notifications: <c>POST /notifications/query</c>.
/// </summary>
/// <remarks>
/// The notifications list, asked for by filter, order, page and field. It answers from the notifications domain
/// table, so no notification is composed. The policy is the list's own, <c>notifications:read</c>. The fields it
/// accepts are declared by the endpoint that registers it
/// (<see cref="QueryDomainEndpointBase{TDomain}.QueryableFields"/>).
/// </remarks>
public abstract class QueryNotificationsEndpointBase : QueryDomainEndpointBase<NotificationDomainConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="QueryNotificationsEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="notificationProvider">Reads the page from the notifications domain table.</param>
    protected QueryNotificationsEndpointBase(ILogger logger, NotificationConfigurationProvider notificationProvider)
        : base(logger, notificationProvider)
    {
    }

    /// <summary>Gets the resource name used for the route and the default policy: <c>/notifications/query</c>.</summary>
    protected override string ResourceName => "notifications";
}
