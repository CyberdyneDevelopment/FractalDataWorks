using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Notifications.Configuration;
using Fdw.Services.Notifications.Endpoints.Logging;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.Extensions.Logging;
using System;
using Fdw.Web.RestEndpoints.Models;

namespace Fdw.Services.Notifications.Endpoints;

/// <summary>
/// Base endpoint for listing notification rules, one page at a time (FDW-951).
/// </summary>
/// <remarks>
/// A page is one read of the notification rules domain table: the fields a rule's summary shows live on the
/// domain row, so no rule's implementation is read to list it (FDW-1005).
/// </remarks>
public abstract class ListNotificationRulesEndpointBase
    : CrudPageEndpointBase<NotificationRuleDomainConfiguration, NotificationRuleSummaryDto>
{
    private readonly INotificationRuleConfigurationProvider _provider;

    /// <inheritdoc />
    protected ListNotificationRulesEndpointBase(ILogger<ListNotificationRulesEndpointBase> logger, INotificationRuleConfigurationProvider provider) : base(logger)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
    }

    /// <inheritdoc />
    protected override string ResourceName => "notifications/rules";

    /// <inheritdoc />
    protected override string EndpointSummary => "List notification rules";

    /// <inheritdoc />
    protected override string EndpointDescription => "Streams one page of notification rules in name order.";

    /// <inheritdoc />
    protected override Task<IGenericResult<ConfigurationPage<NotificationRuleDomainConfiguration>>> LoadPage(
        PaginatedListRequest paging, CancellationToken ct)
    {
        NotificationEndpointLog.ListingNotificationRules(Logger);
        return _provider.Get(new DomainQuery(null, null, paging.Skip, paging.Take, false), ct);
    }

    /// <inheritdoc />
    protected override Task<IGenericResult<NotificationRuleSummaryDto>> ToSummary(NotificationRuleDomainConfiguration item, CancellationToken ct)
        => Task.FromResult(GenericResult<NotificationRuleSummaryDto>.Success(new NotificationRuleSummaryDto
        {
            Id = item.Id,
            Name = item.Name,
            Description = item.Description,
            IsEnabled = item.IsEnabled,
            NotificationServiceType = item.NotificationServiceType,
            NotificationServiceName = item.NotificationServiceName,
            Severity = item.Severity
        }));
}
