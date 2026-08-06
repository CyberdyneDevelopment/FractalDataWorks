using Fdw.Collections;
using Fdw.Results;
using Fdw.Services.Abstractions;

namespace Fdw.Services.Authorization.Abstractions.Paths;

/// <summary>
/// Base class for path authorization policy TypeOptions.
/// Concrete policies override <see cref="Evaluate"/> to return an allow/deny decision.
/// </summary>
public abstract class PathAuthorizationPolicyBase : TypeOptionBase<int, PathAuthorizationPolicyBase>, IPathAuthorizationPolicy
{
    /// <summary>Initializes a new policy with the given id and name.</summary>
    protected PathAuthorizationPolicyBase(int id, string name)
        : base(id, name)
    {
    }

    /// <inheritdoc />
    public abstract IGenericResult<IPathAuthorizationDecision> Evaluate(string canonicalAddress, IRequestContext context);
}
