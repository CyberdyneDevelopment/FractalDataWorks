using Fdw.Collections;
using Fdw.Results;
using Fdw.Services.Abstractions;

namespace Fdw.Services.Authorization.Abstractions.Paths;

/// <summary>
/// Evaluates whether an <see cref="IRequestContext"/> is authorized to access a
/// resolved canonical address. Stateless; produces an <see cref="IPathAuthorizationDecision"/>.
/// </summary>
/// <remarks>
/// Policies are TypeOptions of <see cref="PathAuthorizationPolicies"/>. Downstream projects
/// register their own via <c>[TypeOption(typeof(PathAuthorizationPolicies), "...")]</c>.
/// A DataStore consults the policy assigned to a DataPath at resolution time; the resulting
/// decision is attached to the resolved address.
/// </remarks>
public interface IPathAuthorizationPolicy : ITypeOption<int, IPathAuthorizationPolicy>
{
    /// <summary>
    /// Evaluates the policy against a resolved canonical address and a request context.
    /// </summary>
    /// <param name="canonicalAddress">The fully-resolved server-side address (path / URL / container key).</param>
    /// <param name="context">The request context (tenant, org membership, roles).</param>
    /// <returns>A decision result. Successful + IsAllowed=true means access is granted; failure or IsAllowed=false means denied.</returns>
    IGenericResult<IPathAuthorizationDecision> Evaluate(string canonicalAddress, IRequestContext context);
}
