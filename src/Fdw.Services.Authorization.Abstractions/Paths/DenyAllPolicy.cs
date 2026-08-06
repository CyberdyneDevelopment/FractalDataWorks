using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Services.Abstractions;

namespace Fdw.Services.Authorization.Abstractions.Paths;

/// <summary>
/// Denies all access. Use for sealed paths or as a default-deny fallback when seed data
/// references a policy that no longer exists.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(PathAuthorizationPolicies), "DenyAll")]
public sealed class DenyAllPolicy : PathAuthorizationPolicyBase
{
    /// <summary>Initializes the DenyAll policy.</summary>
    public DenyAllPolicy() : base(4, "DenyAll") { }

    /// <inheritdoc />
    public override IGenericResult<IPathAuthorizationDecision> Evaluate(string canonicalAddress, IRequestContext context)
        => GenericResult<IPathAuthorizationDecision>.Success(
            PathAuthorizationDecision.Deny(Name, "DenyAll policy: no access permitted."));
}
