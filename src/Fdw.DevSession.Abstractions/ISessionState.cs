using Fdw.Collections;

namespace Fdw.DevSession.Abstractions;

/// <summary>
/// A lifecycle state of a dev session.
/// </summary>
/// <remarks>
/// This is an OPEN collection (<c>[MutableTypeCollection]</c>): a consumer can register additional
/// states from its own assembly with a <c>[TypeOption(typeof(SessionStates), "...")]</c> option and
/// they are discovered across assemblies. The built-in set (open, sleeping, hibernated, blocked,
/// merging, done) is a starting point, not a closed enumeration.
/// </remarks>
public interface ISessionState : ITypeOption<int, SessionStateBase>
{
    /// <summary>
    /// Gets a value indicating whether this is a terminal state — the session is finished and cannot
    /// transition further.
    /// </summary>
    bool IsTerminal { get; }

    /// <summary>
    /// Gets a value indicating whether a session in this state may be reclaimed (its warm resources freed)
    /// without losing durable progress — e.g. sleeping, hibernated, or blocked-on-a-human states.
    /// </summary>
    bool IsReclaimable { get; }
}
