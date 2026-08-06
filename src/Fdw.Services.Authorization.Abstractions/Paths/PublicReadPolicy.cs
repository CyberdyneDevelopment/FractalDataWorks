using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Services.Abstractions;

namespace Fdw.Services.Authorization.Abstractions.Paths;

/// <summary>
/// Allows access to anyone. Read-only by convention; the DataStore enforces write-vs-read
/// at the operation site. Use for genuinely public content (documentation, samples).
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(PathAuthorizationPolicies), "PublicRead")]
public sealed class PublicReadPolicy : PathAuthorizationPolicyBase
{
    /// <summary>Initializes the PublicRead policy.</summary>
    public PublicReadPolicy() : base(3, "PublicRead") { }

    /// <inheritdoc />
    public override IGenericResult<IPathAuthorizationDecision> Evaluate(string canonicalAddress, IRequestContext context)
        => GenericResult<IPathAuthorizationDecision>.Success(PathAuthorizationDecision.Allow(Name));
}
