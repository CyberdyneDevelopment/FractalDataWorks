using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Authorization.Abstractions;
using Fdw.Services.Etl.Abstractions.Execution;
using Fdw.Services.Scheduling.Clients.Abstractions;
using Fdw.Services.Scheduling.Endpoints.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Scheduling.Endpoints;

/// <summary>
/// Issues the grant a person states when they save a schedule whose runs are to act as them (FDW-6144): the one place the rules of saving such a schedule are judged, for the create and the update alike.
/// </summary>
/// <remarks>
/// The person is the signed-in caller (<see cref="IRunGrants.Issue"/> reads them from the caller, never from the request). The caller must hold <see cref="Permission"/>, hold every permission they
/// allow (a grant never allows more than the person who gives it holds), and allow at least everything the schedule's run roles carry, because a fire judged against a grant that allowed less would be
/// refused every time.
/// </remarks>
public sealed class ScheduleRunAsGrants
{
    /// <summary>The permission that lets a person have a schedule's runs act as them.</summary>
    public const string Permission = "schedules:run-as-me";

    private readonly IRunGrants _grants;
    private readonly IRoleGrants _roleGrants;

    /// <summary>Initializes a new instance of the <see cref="ScheduleRunAsGrants"/> class.</summary>
    /// <param name="grants">Issues the grant.</param>
    /// <param name="roleGrants">Says what permissions the run roles carry.</param>
    public ScheduleRunAsGrants(IRunGrants grants, IRoleGrants roleGrants)
    {
        _grants = grants;
        _roleGrants = roleGrants;
    }

    /// <summary>Issues the grant a request states.</summary>
    /// <param name="caller">The signed-in caller, who is the person.</param>
    /// <param name="scheduleName">The schedule, for the refusals to name.</param>
    /// <param name="runAs">What the person states.</param>
    /// <param name="runRoles">The roles the schedule's runs act with once the request is saved.</param>
    /// <param name="logger">The endpoint's logger.</param>
    /// <param name="cancellationToken">Cancels the calls.</param>
    /// <returns>The grant; or the refusal.</returns>
    public async Task<IGenericResult<RunGrant>> Issue(
        IAuthenticationContext caller, string scheduleName, ScheduleRunAsRequest runAs, IReadOnlyList<string> runRoles, ILogger logger, CancellationToken cancellationToken = default)
    {
        if (!PermissionCoverage.Covers(caller.Permissions, Permission))
        {
            return GenericResult<RunGrant>.Failure(ScheduleEndpointLog.RunAsNotPermitted(logger, scheduleName, Permission));
        }

        if (runAs.ExpiresAt is not { } expiresAt)
        {
            return GenericResult<RunGrant>.Failure(ScheduleEndpointLog.RunAsExpiryMissing(logger, scheduleName));
        }

        var stated = runAs.Permissions ?? [];
        var notHeld = stated.Where(permission => !PermissionCoverage.Covers(caller.Permissions, permission)).ToList();
        if (notHeld.Count > 0)
        {
            return GenericResult<RunGrant>.Failure(ScheduleEndpointLog.RunAsExceedsPerson(logger, scheduleName, string.Join(", ", notHeld)));
        }

        var carried = await _roleGrants.Get(runRoles, cancellationToken).ConfigureAwait(false);
        if (carried.IsFailure || carried.Value is not { } rolePermissions)
        {
            return carried.ToNewResult<RunGrant>();
        }

        var notAllowed = rolePermissions.Where(permission => !PermissionCoverage.Covers(stated, permission)).Distinct(System.StringComparer.OrdinalIgnoreCase).ToList();
        return notAllowed.Count > 0
            ? GenericResult<RunGrant>.Failure(ScheduleEndpointLog.RunAsNotCovering(logger, scheduleName, string.Join(", ", notAllowed)))
            : await _grants.Issue([.. stated], expiresAt, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Takes a grant back after the schedule that stored it no longer does; the grant service records a failure, and the request that changed the schedule is not failed by it.</summary>
    /// <param name="grantId">The grant, or none.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The task.</returns>
    public async Task Revoke(System.Guid? grantId, CancellationToken cancellationToken = default)
    {
        if (grantId is { } id)
        {
            _ = await _grants.Revoke(id, cancellationToken).ConfigureAwait(false);
        }
    }
}
