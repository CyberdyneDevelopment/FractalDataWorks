using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Services.Authentication.Abstractions;

namespace Fdw.Services.Multitenancy.Abstractions.Scoping;

/// <summary>
/// Scoping by visibility group within a tenant.
/// </summary>
/// <remarks>
/// The second argument to <c>security.fn_TenantFilter</c>. It is what the session context calls
/// <c>VisibilityGroupId</c> and what <c>IAuthenticationContext</c> has historically called
/// <c>ActiveOrgId</c> — three names for one dimension, which is what naming it once here fixes.
/// </remarks>
[TypeOption(typeof(ScopeLayers), "VisibilityGroup")]
public sealed class VisibilityGroupScopeLayer : ScopeLayerBase
{
    /// <summary>Initializes a new instance of the <see cref="VisibilityGroupScopeLayer"/> class.</summary>
    public VisibilityGroupScopeLayer()
        : base(
            id: 2,
            name: "VisibilityGroup",
            claim: ClaimDefinitions.orgId,
            columnName: "VisibilityGroupId",
            sessionContextKey: "VisibilityGroupId")
    {
    }
}
