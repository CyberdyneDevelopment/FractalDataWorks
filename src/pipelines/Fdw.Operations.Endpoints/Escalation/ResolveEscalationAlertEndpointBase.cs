using System;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Operations.Abstractions.Escalation;

namespace Fdw.Operations.Endpoints.Escalation;

/// <summary>
/// Abstract endpoint that resolves an escalation alert (FDW-3863): <c>POST /escalation/alerts/{Id}/resolve</c>, with no body. Who acted is the caller's identity. Answers the alert;
/// 404 when it is not found for the caller's tenant and 409 when its state does not allow it. Needs the permission <c>escalation:acknowledge</c>.
/// </summary>
public abstract class ResolveEscalationAlertEndpointBase : Endpoint<EscalationAlertIdRequest, EscalationAlertResponse>
{
    private readonly IEscalationAlerts _alerts;

    /// <summary>Initializes a new instance of the <see cref="ResolveEscalationAlertEndpointBase"/> class.</summary>
    /// <param name="alerts">The alerts.</param>
    protected ResolveEscalationAlertEndpointBase(IEscalationAlerts alerts)
    {
        _alerts = alerts ?? throw new ArgumentNullException(nameof(alerts));
    }

    /// <summary>Configures the route, the permission and the OpenAPI metadata.</summary>
    public override void Configure()
    {
        Post("/escalation/alerts/{Id}/resolve");
        Policies("escalation:acknowledge");
        Summary(s => s.Summary = "Resolve an escalation alert");
        ConfigureEndpoint();
    }

    /// <summary>Override to add tags or additional endpoint configuration.</summary>
    protected virtual void ConfigureEndpoint() { }

    /// <inheritdoc />
    public override async Task HandleAsync(EscalationAlertIdRequest req, CancellationToken ct)
    {
        if (User.Identity is not { IsAuthenticated: true, Name: { Length: > 0 } actor })
        {
            await Send.UnauthorizedAsync(ct).ConfigureAwait(false);
            return;
        }

        var done = await _alerts.Resolve(req.Id, actor, ct).ConfigureAwait(false);
        if (done.IsFailure || done.Value is null)
        {
            if (done.CurrentMessage is { } text)
            {
                AddError(text);
            }

            await Send.ErrorsAsync(EscalationAlertFailure.StatusOf(done), ct).ConfigureAwait(false);
            return;
        }

        await Send.OkAsync(EscalationAlertResponse.Of(done.Value), ct).ConfigureAwait(false);
    }
}
