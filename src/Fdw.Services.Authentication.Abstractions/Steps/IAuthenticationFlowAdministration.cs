using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Authentication.Abstractions.Steps;

/// <summary>
/// Changes who may use each sign-in flow: on or off, open to every active user or limited to holders of a permission.
/// </summary>
/// <remarks>
/// <para>
/// What an administration console calls instead of editing flow rows. Every change is written through
/// the flow's configuration provider (versioned like any other configuration write) and then reloaded
/// into this host's flows, so it takes effect on this host without a restart. Another host serving the
/// same flows picks the change up when it restarts.
/// </para>
/// <para>
/// A flow is limited by a permission-gated step (<see cref="IPermissionGatedAuthenticationStep"/>); this
/// places, rewrites or removes that step. It places and rewrites only the one step registered as an
/// <see cref="IAdministrablePermissionGate"/>, at the first position where the flow has established what
/// the step reads, which puts it straight after the permissions are baked.
/// </para>
/// <para>
/// Everything a flow can express stays expressible in configuration; this refuses only what would leave
/// the flow or the administrator unable to operate. It will not limit a flow to a permission no role
/// carries (the flow would admit nobody), and it will not make a restrictive change that leaves the
/// administrator making it no enabled flow that proves how they signed in and admits them.
/// </para>
/// </remarks>
public interface IAuthenticationFlowAdministration
{
    /// <summary>Returns every configured flow, switched on or not, loaded or not.</summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    Task<IGenericResult<IReadOnlyList<AuthenticationFlowState>>> Get(CancellationToken cancellationToken = default);

    /// <summary>Switches a flow on or off.</summary>
    /// <param name="flowName">The flow.</param>
    /// <param name="isEnabled">True to switch it on.</param>
    /// <param name="administrator">Who is making the change.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// The flow as it now stands. Failures are coded: <c>FlowNotFound</c>, and <c>ChangeLocksOutAdministrator</c>
    /// when switching it off would leave the administrator no way to sign in the way they did.
    /// </returns>
    Task<IGenericResult<AuthenticationFlowState>> SetEnabled(
        string flowName, bool isEnabled, AuthenticationFlowAdministrator administrator, CancellationToken cancellationToken = default);

    /// <summary>Limits a flow to principals holding <paramref name="permission"/>.</summary>
    /// <param name="flowName">The flow.</param>
    /// <param name="permission">A permission this platform defines and some role carries.</param>
    /// <param name="administrator">Who is making the change.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// The flow as it now stands. Failures are coded: <c>FlowNotFound</c>, <c>PermissionNotDefined</c>,
    /// <c>PermissionHeldByNoRole</c>, <c>FlowCannotBeGated</c> when nothing in the flow establishes what a
    /// gate reads or no administrable gate is registered, <c>FlowGateAmbiguous</c> when the flow already
    /// requires several permissions, <c>FlowGateNotAdministrable</c> when its gate is one this host cannot
    /// configure, and <c>ChangeLocksOutAdministrator</c>.
    /// </returns>
    Task<IGenericResult<AuthenticationFlowState>> RequirePermission(
        string flowName, string permission, AuthenticationFlowAdministrator administrator, CancellationToken cancellationToken = default);

    /// <summary>Opens a flow to every active user by removing its permission gate.</summary>
    /// <param name="flowName">The flow.</param>
    /// <param name="administrator">Who is making the change.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// The flow as it now stands; a flow already open is returned unchanged. Refused, coded
    /// <c>FlowGateAmbiguous</c>, when the flow requires several permissions, for the same reason setting one is.
    /// Opening only widens who may sign in, so it is never refused as a lock-out.
    /// </returns>
    Task<IGenericResult<AuthenticationFlowState>> Open(
        string flowName, AuthenticationFlowAdministrator administrator, CancellationToken cancellationToken = default);
}
