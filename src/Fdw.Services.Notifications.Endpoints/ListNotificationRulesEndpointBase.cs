using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Notifications.Configuration;
using Fdw.Services.Notifications.Endpoints.Logging;
using Fdw.Web.RestEndpoints.Crud;

namespace Fdw.Services.Notifications.Endpoints;

/// <summary>
/// Base endpoint for listing all notification rules.
/// </summary>
public abstract class ListNotificationRulesEndpointBase : CrudListEndpoint<NotificationRuleSummaryDto>
{
    private readonly IServiceConfigurationProvider<NotificationRuleConfiguration> _provider;

    /// <inheritdoc />
    protected ListNotificationRulesEndpointBase(IServiceConfigurationProvider<NotificationRuleConfiguration> provider)
    {
        _provider = provider;
    }

    /// <inheritdoc />
    protected override string ResourceName => "notifications/rules";

    /// <inheritdoc />
    protected override string EndpointSummary => "List notification rules";

    /// <inheritdoc />
    protected override string EndpointDescription => "Returns all notification rules.";

    /// <inheritdoc />
    protected override async Task<IGenericResult<List<NotificationRuleSummaryDto>>> LoadItems(CancellationToken ct)
    {
        NotificationEndpointLog.ListingNotificationRules(Logger);

        var allResult = await _provider.Get(ct).ConfigureAwait(false);
        if (!allResult.IsSuccess)
        {
            return allResult.ToNewResult<List<NotificationRuleSummaryDto>>();
        }

        var items = (allResult.Value ?? (IReadOnlyList<NotificationRuleConfiguration>)[])
            .Select(r => new NotificationRuleSummaryDto
            {
                Id = r.Id,
                Name = r.Name,
                Description = r.Description,
                IsEnabled = r.IsEnabled,
                NotificationServiceType = r.NotificationServiceType,
                NotificationServiceName = r.NotificationServiceName,
                Severity = r.Severity
            })
            .ToList();

        return GenericResult<List<NotificationRuleSummaryDto>>.Success(items);
    }
}
