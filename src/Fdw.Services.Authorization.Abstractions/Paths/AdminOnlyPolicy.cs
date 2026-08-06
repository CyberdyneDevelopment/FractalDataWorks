using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Services.Abstractions;

namespace Fdw.Services.Authorization.Abstractions.Paths;

/// <summary>
/// Allows access only when the caller has the system-admin role. Use for sensitive paths
/// like <c>AllProjects</c> that span all tenants.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(PathAuthorizationPolicies), "AdminOnly")]
public sealed class AdminOnlyPolicy : PathAuthorizationPolicyBase
{
    /// <summary>Initializes the AdminOnly policy.</summary>
    public AdminOnlyPolicy() : base(2, "AdminOnly") { }

    /// <inheritdoc />
    public override IGenericResult<IPathAuthorizationDecision> Evaluate(string canonicalAddress, IRequestContext context)
    {
        if (context.IsSystemAdmin)
            return GenericResult<IPathAuthorizationDecision>.Success(PathAuthorizationDecision.Allow(Name));

        return GenericResult<IPathAuthorizationDecision>.Success(
            PathAuthorizationDecision.Deny(Name, "System-admin role required."));
    }
}
