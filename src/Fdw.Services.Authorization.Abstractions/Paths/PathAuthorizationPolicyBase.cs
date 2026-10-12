using Fdw.Collections;
using Fdw.Results;
using Fdw.Services.Abstractions;

namespace Fdw.Services.Authorization.Abstractions.Paths;

/// <summary>
/// Base class for path authorization policy TypeOptions.
/// Concrete policies override <see cref="Evaluate"/> to return an allow/deny decision.
/// </summary>
public abstract class PathAuthorizationPolicyBase : TypeOptionBase<PathAuthorizationPolicyBase>, IPathAuthorizationPolicy
{
    /// <summary>Initializes a new policy with the given id and name.</summary>
    protected PathAuthorizationPolicyBase(string name)
        : base(name)
    {
    }

    /// <inheritdoc />
    public abstract IGenericResult<IPathAuthorizationDecision> Evaluate(string canonicalAddress, IRequestContext context);
}
