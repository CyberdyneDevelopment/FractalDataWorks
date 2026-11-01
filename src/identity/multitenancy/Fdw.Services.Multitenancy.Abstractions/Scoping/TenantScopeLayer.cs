using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Claims.Abstractions;

namespace Fdw.Services.Multitenancy.Abstractions.Scoping;

/// <summary>
/// Scoping by tenant.
/// </summary>
[TypeOption(typeof(ScopeLayers), "Tenant")]
public sealed class TenantScopeLayer : ScopeLayerBase
{
    /// <summary>Initializes a new instance of the <see cref="TenantScopeLayer"/> class.</summary>
    public TenantScopeLayer()
        : base(
            name: "Tenant",
            claim: ClaimDefinitions.tenantId,
            columnName: "TenantId",
            sessionContextKey: "TenantId")
    {
    }
}
