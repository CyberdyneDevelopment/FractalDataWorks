using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Credentials.Abstractions.Outcomes;

/// <summary>
/// TypeCollection of credential validation outcomes. The interface and base live here in
/// <c>.Abstractions</c>; the concrete <c>[TypeOption]</c>s (Match / NoMatch / Expired /
/// TooManyAttempts / MustChange) are declared in the implementation library, so the set is
/// downstream-extensible. The source generator provides compile-time discovery and O(1)
/// <c>ByName</c>/<c>ById</c> lookups (including cross-assembly options).
/// </summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(CredentialOutcomeBase), typeof(ICredentialOutcome), typeof(CredentialOutcomes))]
public sealed partial class CredentialOutcomes : TypeCollectionBase<CredentialOutcomeBase, ICredentialOutcome>
{
}
