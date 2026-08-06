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
    public TenantScopedPolicy() : base(1, "TenantScoped") { }

    /// <inheritdoc />
    public override IGenericResult<IPathAuthorizationDecision> Evaluate(string canonicalAddress, IRequestContext context)
    {
        if (context.IsSystemAdmin)
            return GenericResult<IPathAuthorizationDecision>.Success(PathAuthorizationDecision.Allow(Name));

        // Why: tenant id is a guid; the resolved address must contain it as a path segment.
        var tenantSegment = context.TenantId.ToString("D", System.Globalization.CultureInfo.InvariantCulture);
        if (canonicalAddress.IndexOf(tenantSegment, StringComparison.OrdinalIgnoreCase) >= 0)
            return GenericResult<IPathAuthorizationDecision>.Success(PathAuthorizationDecision.Allow(Name));

        return GenericResult<IPathAuthorizationDecision>.Success(
            PathAuthorizationDecision.Deny(Name, $"Address does not contain tenant segment '{tenantSegment}'."));
    }
}
