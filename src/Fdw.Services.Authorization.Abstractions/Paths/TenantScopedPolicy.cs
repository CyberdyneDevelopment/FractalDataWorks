using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Services.Abstractions;

namespace Fdw.Services.Authorization.Abstractions.Paths;

/// <summary>
/// Allows access when the resolved address contains the caller's TenantId as a path segment
/// (or matches anywhere if the caller is a system admin). Use when paths are scoped per-tenant
/// (e.g., <c>{tenantId}/projects/{projectName}/{filename}</c>).
/// </summary>
/// <remarks>
/// Owner-per-user scoping (<c>{userId}</c>) is intentionally not implemented in this slice;
/// IRequestContext doesn't yet expose UserId. When that lands a separate OwnerOnlyPolicy
/// will sit alongside this one.
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(PathAuthorizationPolicies), "TenantScoped")]
public sealed class TenantScopedPolicy : PathAuthorizationPolicyBase
{
    /// <summary>Initializes the TenantScoped policy.</summary>
    public TenantScopedPolicy() : base("TenantScoped") { }

    /// <inheritdoc />
    public override IGenericResult<IPathAuthorizationDecision> Evaluate(string canonicalAddress, IRequestContext context)
    {
        if (context.IsPlatformAdmin)
            return GenericResult<IPathAuthorizationDecision>.Success(PathAuthorizationDecision.Allow(Name));

        // A caller acting in no tenant has no tenant segment to match. It used to be matched as
        // Guid.Empty's text, which no address contains -- the same deny, but for a reason nothing
        // said out loud.
        if (context.TenantId is not { } tenantId)
            return GenericResult<IPathAuthorizationDecision>.Success(
                PathAuthorizationDecision.Deny(Name, "The caller is not acting in a tenant."));

        var tenantSegment = tenantId.ToString("D", System.Globalization.CultureInfo.InvariantCulture);
        if (canonicalAddress.IndexOf(tenantSegment, StringComparison.OrdinalIgnoreCase) >= 0)
            return GenericResult<IPathAuthorizationDecision>.Success(PathAuthorizationDecision.Allow(Name));

        return GenericResult<IPathAuthorizationDecision>.Success(
            PathAuthorizationDecision.Deny(Name, $"Address does not contain tenant segment '{tenantSegment}'."));
    }
}
