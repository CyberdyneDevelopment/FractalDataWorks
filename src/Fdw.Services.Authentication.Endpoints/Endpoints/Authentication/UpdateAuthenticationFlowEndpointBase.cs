using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Authentication.Abstractions.Steps;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Authentication.Endpoints;

/// <summary>
/// Generic base endpoint switching a sign-in flow on or off (PATCH /auth/flows/{Name}).
/// </summary>
/// <remarks>
/// Switching a flow off keeps its row, so switching it back on restores it exactly. A switched-off flow
/// is refused to callers, is not offered, and its sessions and unredeemed one-time codes stop working.
/// </remarks>
public abstract class UpdateAuthenticationFlowEndpointBase : AuthenticationFlowChangeEndpointBase<UpdateAuthenticationFlowRequest>
{
    /// <summary>Initializes a new instance of the <see cref="UpdateAuthenticationFlowEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="administration">Makes the change.</param>
    protected UpdateAuthenticationFlowEndpointBase(ILogger logger, IAuthenticationFlowAdministration administration)
        : base(logger, administration)
    {
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Patch("/auth/flows/{Name}");
        Summary(s =>
        {
            s.Summary = "Switch a sign-in flow on or off";
            s.Description = "Body {isEnabled}. Takes effect on this host at once; another host serving the same flows picks it up when it restarts.";
        });
        ConfigureChange();
    }

    /// <inheritdoc />
    public override async Task HandleAsync(UpdateAuthenticationFlowRequest req, CancellationToken ct)
    {
        if (req.IsEnabled is not { } isEnabled)
        {
            AddError(r => r.IsEnabled, "isEnabled is required: it is the only thing this route changes.");
            await Send.ErrorsAsync(400, ct).ConfigureAwait(false);
            return;
        }

        AuthenticationEndpointLog.FlowChangeRequested(EndpointLogger, isEnabled ? "switch on" : "switch off", req.Name);
        await SendChange(req.Name, administrator => Administration.SetEnabled(req.Name, isEnabled, administrator, ct), ct).ConfigureAwait(false);
    }
}
