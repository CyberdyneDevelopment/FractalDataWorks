using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Authentication.Abstractions.Results;

/// <summary>
/// No role carries the permission a flow was to be limited to.
/// </summary>
/// <remarks>
/// Limiting a flow to it would admit nobody. The permission exists, so this is a state to fix first -- grant it to a role -- rather than bad input. An endpoint answers it with a 409.
/// </remarks>
[TypeOption(typeof(AuthenticationResultCodes), "PermissionHeldByNoRole", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class PermissionHeldByNoRoleCode : AuthenticationResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="PermissionHeldByNoRoleCode"/> class.</summary>
    public PermissionHeldByNoRoleCode()
        : base(41002, "PermissionHeldByNoRole",
            ResultSeverities.ByName("Error"),
            "No role carries that permission, so the flow would admit nobody",
            isRetryable: false)
    {
    }
}
