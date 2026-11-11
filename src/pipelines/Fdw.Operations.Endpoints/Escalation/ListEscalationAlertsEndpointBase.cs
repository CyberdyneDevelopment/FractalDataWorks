using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Operations.Abstractions.Escalation;
using Fdw.Services.Settings;
using Fdw.Web.RestEndpoints.Pagination;
using Fdw.Web.RestEndpoints.ErrorMapping;
using Fdw.Results;

namespace Fdw.Operations.Endpoints.Escalation;

/// <summary>
/// Abstract endpoint that lists the alerts escalation policies walk (FDW-3863): <c>GET /escalation/alerts</c>, newest first, narrowed by the query and paged by <c>skip</c> and
/// <c>take</c>. Needs the permission <c>escalation:read</c>; the alerts of other tenants are not listed.
/// </summary>
public abstract class ListEscalationAlertsEndpointBase : Endpoint<EscalationAlertListRequest, EscalationAlertListResponse>
{
    private readonly IEscalationAlerts _alerts;

    /// <summary>Initializes a new instance of the <see cref="ListEscalationAlertsEndpointBase"/> class.</summary>
    /// <param name="alerts">The alerts.</param>
    protected ListEscalationAlertsEndpointBase(IEscalationAlerts alerts)
    {
        _alerts = alerts;
    }

    /// <summary>Configures the route, the permission and the OpenAPI metadata.</summary>
    public override void Configure()
    {
        Get("/escalation/alerts");
        Policies("escalation:read");
        Summary(s => s.Summary = "List escalation alerts");
        ConfigureEndpoint();
    }

    /// <summary>Override to add tags or additional endpoint configuration.</summary>
    protected virtual void ConfigureEndpoint() { }

    /// <inheritdoc />
    public override async Task HandleAsync(EscalationAlertListRequest req, CancellationToken ct)
    {
        if (req.State is not null && req.Open is not null)
        {
            await Send.ResultAsync(ResultHttpStatusMapper.Answer(GenericResult.Failure(OperationsEndpointLog.ListEscalationAlertsStateAndOpenTogether(Logger)), HttpContext)).ConfigureAwait(false);
            return;
        }

        var listed = await _alerts.List(req.State, ct).ConfigureAwait(false);
        if (listed.IsFailure)
        {
            await Send.ResultAsync(ResultHttpStatusMapper.Answer(listed, HttpContext)).ConfigureAwait(false);
            return;
        }

        if (listed.Value is null)
        {
            await Send.ResultAsync(ResultHttpStatusMapper.Answer(GenericResult.Failure(OperationsEndpointLog.ListEscalationAlertsReturnedNothing(Logger)), HttpContext)).ConfigureAwait(false);
            return;
        }

        // Why the shared mechanism: the page the caller asked for, the default and the largest page are the settings every list endpoint reads, and a setting that is not set fails the
        // call and names it, so no page size is assumed here.
        var paging = PaginationQuery.Resolve(HttpContext, Logger, req.Skip.GetValueOrDefault(), req.Take);
        if (paging.IsFailure)
        {
            await Send.ResultAsync(ResultHttpStatusMapper.Answer(paging, HttpContext)).ConfigureAwait(false);
            return;
        }

        if (paging.Value is null)
        {
            await Send.ResultAsync(ResultHttpStatusMapper.Answer(GenericResult.Failure(OperationsEndpointLog.ListEscalationAlertsPagingReturnedNothing(Logger)), HttpContext)).ConfigureAwait(false);
            return;
        }

        var matching = listed.Value.Where(alert => Matches(alert, req)).ToList();
        await Send.OkAsync(
            new EscalationAlertListResponse
            {
                Items = [.. matching.Skip(paging.Value.Skip).Take(paging.Value.Take).Select(EscalationAlertResponse.Of)],
                Total = matching.Count,
                Skip = paging.Value.Skip,
                Take = paging.Value.Take,
                RefreshSeconds = RefreshSeconds(),
            },
            ct).ConfigureAwait(false);
    }

    private static bool Matches(EscalationAlert alert, EscalationAlertListRequest req) =>
        (req.Open is not { } open || open == !string.Equals(alert.State, EscalationAlertStates.Resolved, StringComparison.Ordinal))
        && (req.Policy is null || string.Equals(alert.PolicyName, req.Policy, StringComparison.Ordinal))
        && (req.SubjectKind is null || string.Equals(alert.SubjectKind, req.SubjectKind, StringComparison.Ordinal))
        && (req.SubjectName is null || string.Equals(alert.SubjectName, req.SubjectName, StringComparison.Ordinal));

    // The interval the setting gives the caller's scope; none is assumed.
    private int? RefreshSeconds() => Setting(SettingDefinitions.EscalationAlertsRefreshSeconds);

    private int? Setting(string name) =>
        Resolve<IEffectiveSettingsProvider>().TryGetEffectiveValue<int>(name, out var value) && value > 0 ? value : null;
}
