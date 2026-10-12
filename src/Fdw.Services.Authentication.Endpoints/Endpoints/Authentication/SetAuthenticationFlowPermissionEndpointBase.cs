using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Authentication.Abstractions.Steps;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Authentication.Endpoints;

/// <summary>
/// Generic base endpoint limiting a sign-in flow to holders of one permission
/// (POST /auth/flows/{Name}/required-permission).
/// </summary>
/// <remarks>
/// Answers 400 for a permission the platform does not define, and 409 for a permission no role carries,
/// a flow that cannot take a limit or already requires several permissions, and a limit that would leave
/// the calling administrator no way to sign in the way they did. Who holds the permission is then decided by role
/// grants; the flow is not touched again to admit one more person.
/// </remarks>
public abstract class SetAuthenticationFlowPermissionEndpointBase : AuthenticationFlowChangeEndpointBase<SetAuthenticationFlowPermissionRequest>
{
    /// <summary>Initializes a new instance of the <see cref="SetAuthenticationFlowPermissionEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="administration">Makes the change.</param>
    protected SetAuthenticationFlowPermissionEndpointBase(ILogger logger, IAuthenticationFlowAdministration administration)
        : base(logger, administration)
    {
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Post("/auth/flows/{Name}/required-permission");
        Summary(s =>
        {
            s.Summary = "Limit a sign-in flow to holders of a permission";
            s.Description = "Body {permission}. Only users holding the permission, through a role in the tenant their sign-in lands in, may use the flow.";
        });
        ConfigureChange();
    }

    /// <inheritdoc />
    public override async Task HandleAsync(SetAuthenticationFlowPermissionRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Permission))
        {
            AddError(r => r.Permission, "permission is required.");
            await Send.ErrorsAsync(400, ct).ConfigureAwait(false);
            return;
        }

        AuthenticationEndpointLog.FlowChangeRequested(EndpointLogger, "require '" + req.Permission + "' on", req.Name);
        await SendChange(req.Name, administrator => Administration.RequirePermission(req.Name, req.Permission, administrator, ct), ct).ConfigureAwait(false);
    }
}
