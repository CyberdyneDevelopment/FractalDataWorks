using System;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Operations.Abstractions.Escalation;
using Fdw.Web.RestEndpoints.ErrorMapping;
using Fdw.Results;

namespace Fdw.Operations.Endpoints.Escalation;

/// <summary>
/// Abstract endpoint that acknowledges an escalation alert (FDW-3863): <c>POST /escalation/alerts/{Id}/acknowledge</c>, with no body. Who acted is the caller's identity. Answers the alert;
/// 404 when it is not found for the caller's tenant and 409 when its state does not allow it. Needs the permission <c>escalation:acknowledge</c>.
/// </summary>
public abstract class AcknowledgeEscalationAlertEndpointBase : Endpoint<EscalationAlertIdRequest, EscalationAlertResponse>
{
    private readonly IEscalationAlerts _alerts;

    /// <summary>Initializes a new instance of the <see cref="AcknowledgeEscalationAlertEndpointBase"/> class.</summary>
    /// <param name="alerts">The alerts.</param>
    protected AcknowledgeEscalationAlertEndpointBase(IEscalationAlerts alerts)
    {
        _alerts = alerts;
    }

    /// <summary>Configures the route, the permission and the OpenAPI metadata.</summary>
    public override void Configure()
    {
        Post("/escalation/alerts/{Id}/acknowledge");
        Policies("escalation:acknowledge");
        Summary(s => s.Summary = "Acknowledge an escalation alert");
        ConfigureEndpoint();
    }

    /// <summary>Override to add tags or additional endpoint configuration.</summary>
    protected virtual void ConfigureEndpoint() { }

    /// <inheritdoc />
    public override async Task HandleAsync(EscalationAlertIdRequest req, CancellationToken ct)
    {
        if (User.Identity is not { IsAuthenticated: true, Name: { Length: > 0 } actor })
        {
            await Send.ResultAsync(ResultHttpStatusMapper.Answer(GenericResult.Failure(OperationsEndpointLog.AcknowledgeEscalationAlertNotSignedIn(Logger)), HttpContext)).ConfigureAwait(false);
            return;
        }

        var done = await _alerts.Acknowledge(req.Id, actor, ct).ConfigureAwait(false);
        if (done.IsFailure)
        {
            await Send.ResultAsync(ResultHttpStatusMapper.Answer(done, HttpContext)).ConfigureAwait(false);
            return;
        }

        if (done.Value is null)
        {
            await Send.ResultAsync(ResultHttpStatusMapper.Answer(GenericResult.Failure(OperationsEndpointLog.AcknowledgeEscalationAlertReturnedNothing(Logger, req.Id)), HttpContext)).ConfigureAwait(false);
            return;
        }

        await Send.OkAsync(EscalationAlertResponse.Of(done.Value), ct).ConfigureAwait(false);
    }
}
