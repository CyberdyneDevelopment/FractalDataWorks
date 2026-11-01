using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Identity.JwtAssertion.Assertions;

/// <summary>
/// The ways a federated assertion can reach this process.
/// </summary>
/// <remarks>
/// Extensible by design: a consumer that carries its assertion some other way adds a
/// <c>[TypeOption]</c> against this collection in its own assembly. Nothing here enumerates the
/// known carriers, so nothing has to change when one is added.
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(FederatedAssertionSourceBase), typeof(IFederatedAssertionSource), typeof(FederatedAssertionSources))]
public abstract partial class FederatedAssertionSources : TypeCollectionBase<FederatedAssertionSourceBase, IFederatedAssertionSource>
{
}
