using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Universes.Abstractions;

/// <summary>What happens when someone asks to join a universe.</summary>
/// <remarks>
/// Paired with visibility: a Discoverable universe someone cannot ask to join is a dead end, and an Open one that auto-approves has no gate at all. The pairing is the caller's to choose — neither value is defaulted.
///
/// The matching database CHECK constraint on the column is the backstop, not the definition — it
/// refuses an out-of-set value that reaches the table by some other path. This collection is what
/// an endpoint validates against, so a bad value is rejected with a message naming the option
/// rather than surfacing as a constraint violation.
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(UniverseJoinPolicyBase), typeof(IUniverseJoinPolicy), typeof(UniverseJoinPolicies))]
public abstract partial class UniverseJoinPolicies : TypeCollectionBase<UniverseJoinPolicyBase, IUniverseJoinPolicy>
{
}
