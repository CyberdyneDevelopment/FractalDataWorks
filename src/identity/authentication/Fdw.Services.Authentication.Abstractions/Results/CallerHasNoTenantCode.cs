using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Authentication.Abstractions.Results;

/// <summary>
/// The caller's token names no tenant, so an operation that acts inside the caller's tenant has none to act in.
/// </summary>
/// <remarks>
/// 50001 is the catalog's PermissionDenied (RESULTCODE-CATALOG.md). Its category, Auth, answers 401, as
/// <c>UserResultCodes.MissingTenantClaim</c> (51001) does for the same missing claim. It is refused rather
/// than answered with an empty result, because "your tenant has no organizations" and "you have no
/// tenant" are different facts (FDW-1016).
/// </remarks>
[TypeOption(typeof(AuthenticationResultCodes), "CallerHasNoTenant", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class CallerHasNoTenantCode : AuthenticationResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="CallerHasNoTenantCode"/> class.</summary>
    public CallerHasNoTenantCode()
        : base(50001, "CallerHasNoTenant",
            ResultSeverities.ByName("Error"),
            "The caller's token names no tenant, so '{operation}' has no tenant to act in",
            isRetryable: false)
    {
    }
}
