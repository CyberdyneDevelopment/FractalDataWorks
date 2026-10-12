using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Authentication.Abstractions.Steps;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Authentication.Endpoints;

/// <summary>
/// Generic base endpoint opening a sign-in flow to every active user
/// (DELETE /auth/flows/{Name}/required-permission).
/// </summary>
/// <remarks>A flow already open is answered unchanged; a flow requiring several permissions answers 409.</remarks>
public abstract class RemoveAuthenticationFlowPermissionEndpointBase : AuthenticationFlowChangeEndpointBase<AuthenticationFlowRouteRequest>
{
    /// <summary>Initializes a new instance of the <see cref="RemoveAuthenticationFlowPermissionEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="administration">Makes the change.</param>
    protected RemoveAuthenticationFlowPermissionEndpointBase(ILogger logger, IAuthenticationFlowAdministration administration)
        : base(logger, administration)
    {
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Delete("/auth/flows/{Name}/required-permission");
        Summary(s =>
        {
            s.Summary = "Open a sign-in flow to every active user";
            s.Description = "Removes the flow's permission limit.";
        });
        ConfigureChange();
    }

    /// <inheritdoc />
    public override async Task HandleAsync(AuthenticationFlowRouteRequest req, CancellationToken ct)
    {
        AuthenticationEndpointLog.FlowChangeRequested(EndpointLogger, "open", req.Name);
        await SendChange(req.Name, administrator => Administration.Open(req.Name, administrator, ct), ct).ConfigureAwait(false);
    }
}
