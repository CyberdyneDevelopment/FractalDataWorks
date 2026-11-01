using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Authorization.Abstractions.Paths;

/// <summary>
/// TypeCollection of all <see cref="IPathAuthorizationPolicy"/>s in the system.
/// Downstream projects register policies via <c>[TypeOption(typeof(PathAuthorizationPolicies), "Name")]</c>.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(PathAuthorizationPolicyBase), typeof(IPathAuthorizationPolicy), typeof(PathAuthorizationPolicies))]
public abstract partial class PathAuthorizationPolicies : TypeCollectionBase<PathAuthorizationPolicyBase, IPathAuthorizationPolicy>
{
    // Source generator emits ById / ByName / All / RegisterMember.
}
