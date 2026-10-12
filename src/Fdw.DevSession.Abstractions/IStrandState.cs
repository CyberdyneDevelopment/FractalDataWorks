using Fdw.Collections;

namespace Fdw.DevSession.Abstractions;

/// <summary>
/// A lifecycle state of a concurrent strand within a session.
/// </summary>
/// <remarks>
/// This is an OPEN collection (<c>[MutableTypeCollection]</c>): a consumer can register additional strand
/// states from its own assembly with a <c>[TypeOption(typeof(StrandStates), "...")]</c> option and they
/// are discovered across assemblies. The built-in set (active, reconciling, reconciled, abandoned) is a
/// starting point, not a closed enumeration.
/// </remarks>
public interface IStrandState : ITypeOption<int, StrandStateBase>
{
    /// <summary>
    /// Gets a value indicating whether this is a terminal state — the strand has ended (reconciled or
    /// abandoned) and holds no live scope claim.
    /// </summary>
    bool IsTerminal { get; }
}
