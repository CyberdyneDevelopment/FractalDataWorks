using Fdw.Services.Notifications.Configuration;
using Fdw.Web.RestEndpoints.Query;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Notifications.Endpoints;

/// <summary>
/// Base endpoint for querying notification rules: <c>POST /notifications/rules/query</c>.
/// </summary>
/// <remarks>
/// The notification rules list, asked for by filter, order, page and field. It answers from the notification
/// rules domain table, so no rule's implementation is read. The policy is the list's own,
/// <c>notifications/rules:read</c>. The fields it accepts are declared by the endpoint that registers it
/// (<see cref="QueryDomainEndpointBase{TDomain}.QueryableFields"/>).
/// </remarks>
public abstract class QueryNotificationRulesEndpointBase : QueryDomainEndpointBase<NotificationRuleDomainConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="QueryNotificationRulesEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="ruleProvider">Reads the page from the notification rules domain table.</param>
    protected QueryNotificationRulesEndpointBase(ILogger logger, NotificationRuleConfigurationProvider ruleProvider)
        : base(logger, ruleProvider)
    {
    }

    /// <summary>Gets the resource name used for the route and the default policy: <c>/notifications/rules/query</c>.</summary>
    protected override string ResourceName => "notifications/rules";
}
