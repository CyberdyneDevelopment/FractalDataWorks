using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Services.Abstractions;

namespace Fdw.Services.Authorization.Abstractions.Paths;

/// <summary>
/// Allows access only to the platform administrator — the admin role held while acting in the global
/// tenant. Use for sensitive paths like <c>AllProjects</c> that span all tenants.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(PathAuthorizationPolicies), "AdminOnly")]
public sealed class AdminOnlyPolicy : PathAuthorizationPolicyBase
{
    /// <summary>Initializes the AdminOnly policy.</summary>
    public AdminOnlyPolicy() : base("AdminOnly") { }

    /// <inheritdoc />
    public override IGenericResult<IPathAuthorizationDecision> Evaluate(string canonicalAddress, IRequestContext context)
    {
        if (context.IsPlatformAdmin)
            return GenericResult<IPathAuthorizationDecision>.Success(PathAuthorizationDecision.Allow(Name));

        return GenericResult<IPathAuthorizationDecision>.Success(
            PathAuthorizationDecision.Deny(Name, "Platform administrator required."));
    }
}
