using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Users.Results;

/// <summary>
/// The caller has no tenant_id JWT claim — every user is tenant-scoped, so creating
/// a user without a tenant context is not allowed.
/// </summary>
[TypeOption(typeof(UserResultCodes), "MissingTenantClaim", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class MissingTenantClaimCode : UserResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MissingTenantClaimCode"/> class.
    /// </summary>
    public MissingTenantClaimCode()
        : base(51001, "MissingTenantClaim",
            ResultSeverities.ByName("Error"),
            "Caller has no tenant_id claim; cannot create tenant-scoped user.",
            isRetryable: false)
    {
    }
}
