using System;
using Fdw.Services.Authentication.Abstractions.Security;

namespace Fdw.Services.Pipelines.Runs;

/// <summary>
/// The tenant a caller acts in, for the saves and holds that are judged against it (FDW-6939): the caller's own active tenant, the <c>tenantId</c> claim of its token.
/// </summary>
/// <remarks>
/// Why the token and not the host's tenant context (<c>ITenantContext</c>): that context is filled only by a host whose multitenancy resolves a tenant per request, and the ETL host
/// does not (SingleTenant: <c>EnablesTenantResolution</c> is false), so there it is null for every caller and a save or a hold would refuse an admin whose token states its tenant.
/// The token's active tenant is the one every host has: the trigger reads it for the run's tenant, row-level security reads it, and it is what a hold and the claim of a run must agree on.
/// </remarks>
public static class RunTenant
{
    /// <summary>Gets the tenant a caller acts in.</summary>
    /// <param name="caller">The authenticated caller, or null when there is none.</param>
    /// <returns>The caller's active tenant, or null for a caller whose token states none (or an empty one).</returns>
    public static Guid? Of(IAuthenticationContext? caller) =>
        caller?.ActiveTenantId is { } tenant && tenant != Guid.Empty ? tenant : null;
}
