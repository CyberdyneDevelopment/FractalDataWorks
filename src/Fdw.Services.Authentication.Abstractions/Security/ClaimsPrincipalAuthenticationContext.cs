using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using Fdw.Services.Authentication.Abstractions;
using Fdw.Web.Http.Abstractions.Security;

namespace Fdw.Services.Authentication.Abstractions.Security;

/// <summary>
/// Adapts a <see cref="ClaimsPrincipal"/> into an <see cref="IAuthenticationContext"/> — the single,
/// canonical mapping the whole framework uses. Any authentication provider that sets a ClaimsPrincipal
/// works, so the authorization layer, DataGateway RBAC, and every other consumer stay provider-agnostic.
/// </summary>
/// <remarks>
/// Why this lives in <c>Services.Authentication.Abstractions</c> (public): it is the one place that
/// already sees <see cref="IAuthenticationContext"/>, <see cref="ClaimDefinitions"/>, and
/// <see cref="SecurityMethods"/> together without a reference cycle, and that every consumer already
/// references. A previous design had this internal to <c>Services.Authorization</c>, which forced
/// <c>Services.Data</c> to keep a lossy private clone — exactly the duplication this consolidation removes.
/// </remarks>
[ExcludeFromCodeCoverage]
public sealed class ClaimsPrincipalAuthenticationContext : IAuthenticationContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ClaimsPrincipalAuthenticationContext"/> class.
    /// </summary>
    /// <param name="principal">The claims principal (e.g. from the HTTP context).</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="principal"/> is null.</exception>
    public ClaimsPrincipalAuthenticationContext(ClaimsPrincipal principal)
    {
        if (principal is null)
        {
            throw new ArgumentNullException(nameof(principal));
        }

        IsAuthenticated = principal.Identity?.IsAuthenticated ?? false;

        // Why: read the FDW-baked claims by their CANONICAL ClaimDefinitions names (not string literals)
        // so the reader can never drift from the baker — the same reason tenant/org/crossTenant below use
        // ClaimDefinitions.*.Name. The canonical roles claim is "roles" (plural, isArray); a literal "role"
        // silently misses every baked role. ClaimTypes.* / standard "name" remain as interop fallbacks for
        // non-FDW principals.
        UserId = principal.FindFirst(ClaimDefinitions.sub.Name)?.Value
              ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
              ?? principal.FindFirst("name")?.Value
              ?? string.Empty;

        // Extract Username from "name", falling back to the standard Name claim then UserId.
        Username = principal.FindFirst("name")?.Value
                ?? principal.FindFirst(ClaimTypes.Name)?.Value
                ?? UserId;

        Roles = CollectDistinct(principal, ClaimDefinitions.roles.Name, ClaimTypes.Role);

        // Why: the access token carries one "perm" claim per resolved permission (the baked effective
        // permission set). Enforcement trusts these as authoritative; surface ALL of them because the
        // single-valued Claims dictionary keeps only the first occurrence per type.
        Permissions = principal.FindAll(ClaimDefinitions.perm.Name)
            .Select(c => c.Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        Claims = BuildClaimsMap(principal);
        ExpiresAt = ParseExpiry(principal);

        // Why: ActiveTenantId / ActiveOrgId / IsCrossTenant are read back from the JWT and used by
        // MsSqlConnection to set SESSION_CONTEXT for RLS. Absence of tenant/org is intentional for
        // cross-tenant tokens and system connections; crossTenant=true is baked only when the user
        // holds tenants:view-all and enables RLS Mode 2 (all-tenant access).
        ActiveTenantId = ParseGuidClaim(principal, ClaimDefinitions.tenantId.Name);
        ActiveOrgId = ParseGuidClaim(principal, ClaimDefinitions.orgId.Name);
        IsCrossTenant = string.Equals(
            principal.FindFirst(ClaimDefinitions.crossTenant.Name)?.Value, "true", StringComparison.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public string UserId { get; }

    /// <inheritdoc />
    public string Username { get; }

    /// <inheritdoc />
    public IDictionary<string, object> Claims { get; }

    /// <inheritdoc />
    public IEnumerable<string> Roles { get; }

    /// <inheritdoc />
    public IEnumerable<string> Permissions { get; }

    /// <inheritdoc />
    public bool IsAuthenticated { get; }

    /// <inheritdoc />
    public SecurityMethodBase AuthenticationMethod => (SecurityMethodBase)SecurityMethods.ByName("JWT");

    /// <inheritdoc />
    public DateTimeOffset? ExpiresAt { get; }

    /// <inheritdoc />
    public Guid? ActiveTenantId { get; }

    /// <inheritdoc />
    public Guid? ActiveOrgId { get; }

    /// <inheritdoc />
    public bool IsCrossTenant { get; }

    /// <inheritdoc />
    // Why: an HTTP request authenticated from a ClaimsPrincipal is, by definition, an end-user
    // request — never the deliberate system elevation. System elevation is granted ONLY by
    // SystemAuthenticationContext, constructed explicitly during host bootstrap.
    public bool IsSystemContext => false;

    // Why: helpers keep the constructor under the FDW007 complexity threshold without a
    // ConventionOverride (this assembly does not reference Fdw.Conventions).
    private static List<string> CollectDistinct(ClaimsPrincipal principal, string primaryType, string fallbackType)
        => principal.FindAll(primaryType)
            .Concat(principal.FindAll(fallbackType))
            .Select(c => c.Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

    private static Dictionary<string, object> BuildClaimsMap(ClaimsPrincipal principal)
    {
        var claims = new Dictionary<string, object>(StringComparer.Ordinal);
        foreach (var claim in principal.Claims)
        {
            // First occurrence wins for each claim type.
            if (!claims.ContainsKey(claim.Type))
            {
                claims[claim.Type] = claim.Value;
            }
        }

        return claims;
    }

    private static DateTimeOffset? ParseExpiry(ClaimsPrincipal principal)
    {
        var expClaim = principal.FindFirst("exp")?.Value;
        return expClaim is not null
            && long.TryParse(expClaim, NumberStyles.Integer, CultureInfo.InvariantCulture, out var expUnix)
                ? DateTimeOffset.FromUnixTimeSeconds(expUnix)
                : null;
    }

    private static Guid? ParseGuidClaim(ClaimsPrincipal principal, string claimName)
    {
        var value = principal.FindFirst(claimName)?.Value;
        return value is not null && Guid.TryParse(value, out var parsed) ? parsed : null;
    }
}
