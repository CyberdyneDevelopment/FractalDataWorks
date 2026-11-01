using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Authorization.Abstractions;
using Fdw.Services.Multitenancy.Abstractions;
using Fdw.Services.Workspaces.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Workspaces;

/// <summary>
/// Which place a definition is read or written for, and the scope a place is judged at (FDW-2331, FDW-2560). The organization or tenant comes from who is
/// signed in; a workspace the caller names is accepted only when its tenant is the one they act in, so a caller cannot ask what is above a place of
/// another tenant.
/// </summary>
public static class CallerPlaces
{
    /// <summary>Says which place the caller reads or writes at.</summary>
    /// <param name="authContext">Says who is signed in and the place they act in.</param>
    /// <param name="workspaceId">The workspace the caller names, or none.</param>
    /// <returns>The place, or none when no one is signed in or the caller acts in no tenant.</returns>
    public static CallerPlace? Of(IAuthenticationContextAccessor authContext, Guid? workspaceId)
    {
        if (authContext.Current is not { IsAuthenticated: true, ActiveTenantId: { } tenantId } caller)
        {
            return null;
        }

        if (workspaceId is { } workspace)
        {
            return new CallerPlace(PlaceKinds.Workspace, workspace, tenantId);
        }

        return caller.ActiveOrgId is { } organizationId
            ? new CallerPlace(PlaceKinds.Organization, organizationId, tenantId)
            : new CallerPlace(PlaceKinds.Tenant, tenantId, tenantId);
    }

    /// <summary>Reads the places at or above the place the caller acts in, nearest first.</summary>
    /// <param name="ancestry">Says which places are at or above a place.</param>
    /// <param name="origin">The place.</param>
    /// <param name="logger">The logger a refusal is written to.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The places, or the failure when they cannot be read or the place is not in the caller's own tenant.</returns>
    public static async Task<IGenericResult<IReadOnlyList<PlaceStep>>> Above(
        IPlaceAncestry ancestry, CallerPlace origin, ILogger logger, CancellationToken cancellationToken = default)
    {
        var steps = await ancestry.Get(origin.Kind, origin.Id, cancellationToken).ConfigureAwait(false);
        if (steps.IsFailure)
        {
            return steps;
        }

        return steps.Value is { Count: > 0 } places && TenantOf(places) == origin.TenantId
            ? steps
            : GenericResult<IReadOnlyList<PlaceStep>>.Failure(PlaceAncestryLog.NotInTenant(logger, $"{origin.Kind} '{origin.Id:D}'"));
    }

    /// <summary>Finds the tenant among the places.</summary>
    /// <param name="steps">The places at or above a place.</param>
    /// <returns>The tenant's id, or none when the places hold no tenant.</returns>
    public static Guid? TenantOf(IReadOnlyList<PlaceStep> steps)
        => steps.FirstOrDefault(step => string.Equals(step.Kind, PlaceKinds.Tenant, StringComparison.Ordinal))?.Id;

    /// <summary>Says the scope a place is judged at: its tenant, and the organization it is or sits in.</summary>
    /// <param name="steps">The places at or above a place.</param>
    /// <param name="step">One of them.</param>
    /// <returns>The scope, or none when the places hold no tenant.</returns>
    public static ThingScope? ScopeOf(IReadOnlyList<PlaceStep> steps, PlaceStep step)
    {
        if (TenantOf(steps) is not { } tenantId)
        {
            return null;
        }

        if (string.Equals(step.Kind, PlaceKinds.Tenant, StringComparison.Ordinal))
        {
            return new ThingScope(tenantId, null);
        }

        var organization = string.Equals(step.Kind, PlaceKinds.Organization, StringComparison.Ordinal)
            ? step
            : steps.FirstOrDefault(above => above.Depth > step.Depth && string.Equals(above.Kind, PlaceKinds.Organization, StringComparison.Ordinal));
        return new ThingScope(tenantId, organization?.Id);
    }

    /// <summary>
    /// Says whether the caller may write a version at a place: it is an organization or a workspace, it is in the caller's tenant, and the right is held
    /// there. A place of another tenant and a place that does not exist are refused alike.
    /// </summary>
    /// <param name="ancestry">Says which places are at or above a place.</param>
    /// <param name="authority">Says whether the caller holds a right at a place.</param>
    /// <param name="placeKind">The kind of place.</param>
    /// <param name="placeId">The id of the place.</param>
    /// <param name="callerTenantId">The tenant the caller acts in.</param>
    /// <param name="writeRight">The right that lets the caller write at a place.</param>
    /// <param name="logger">The logger a refusal is written to.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Success when the caller may write there; otherwise why not.</returns>
    public static async Task<IGenericResult> CanWrite(
        IPlaceAncestry ancestry,
        IScopeAuthority authority,
        string placeKind,
        Guid placeId,
        Guid callerTenantId,
        string writeRight,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        var versionable = string.Equals(placeKind, PlaceKinds.Organization, StringComparison.Ordinal)
            || string.Equals(placeKind, PlaceKinds.Workspace, StringComparison.Ordinal);
        if (!versionable)
        {
            return GenericResult.Failure(PlaceAncestryLog.NotVersionable(logger, placeKind));
        }

        var steps = await ancestry.Get(placeKind, placeId, cancellationToken).ConfigureAwait(false);
        if (steps.IsFailure)
        {
            return steps;
        }

        if (steps.Value is not { Count: > 0 } places || TenantOf(places) != callerTenantId || ScopeOf(places, places[0]) is not { } scope)
        {
            return GenericResult.Failure(PlaceAncestryLog.NotInTenant(logger, $"{placeKind} '{placeId:D}'"));
        }

        return await authority.May(writeRight, scope, cancellationToken).ConfigureAwait(false);
    }
}
