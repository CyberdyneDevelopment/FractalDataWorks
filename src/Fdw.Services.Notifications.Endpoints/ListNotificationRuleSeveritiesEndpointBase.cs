using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Notifications.Endpoints;

/// <summary>
/// Base endpoint serving the severities a notification rule may carry: <c>GET /notifications/rules/severities</c> (FDW-2931).
/// </summary>
public abstract class ListNotificationRuleSeveritiesEndpointBase : CrudListEndpointBase<NotificationRuleSeverityDto>
{
    /// <summary>Initializes a new instance of the <see cref="ListNotificationRuleSeveritiesEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    protected ListNotificationRuleSeveritiesEndpointBase(ILogger<ListNotificationRuleSeveritiesEndpointBase> logger) : base(logger)
    {
    }

    /// <inheritdoc />
    protected override string ResourceName => "notifications/rules/severities";

    /// <inheritdoc />
    protected override string ReadPolicy => "notifications/rules:read";

    /// <inheritdoc />
    protected override string EndpointSummary => "List the severities a notification rule may carry";

    /// <inheritdoc />
    protected override string EndpointDescription => "Returns each severity a rule may carry with the priority a notification sent through it has. A rule that states another severity is refused.";

    /// <inheritdoc />
    protected override Task<IGenericResult<List<NotificationRuleSeverityDto>>> LoadItems(CancellationToken ct)
        => Task.FromResult(GenericResult<List<NotificationRuleSeverityDto>>.Success(
            [.. NotificationRuleSeverities.All().Select(pair => new NotificationRuleSeverityDto { Name = pair.Key, Priority = pair.Value })]));
}
