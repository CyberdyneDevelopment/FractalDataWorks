using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;

namespace Fdw.DevSession.Abstractions;

/// <summary>
/// CRTP base class for <see cref="ISessionState"/> options. Each concrete state supplies its id, name,
/// and whether it is terminal and/or reclaimable.
/// </summary>
[ExcludeFromCodeCoverage]
public abstract class SessionStateBase : TypeOptionBase<SessionStateBase>, ISessionState
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SessionStateBase"/> class.
    /// </summary>
    /// <param name="name">The state name.</param>
    /// <param name="isTerminal">Whether this is a terminal state.</param>
    /// <param name="isReclaimable">Whether a session in this state may be reclaimed without losing durable progress.</param>
    protected SessionStateBase(string name, bool isTerminal, bool isReclaimable)
        : base(name)
    {
        IsTerminal = isTerminal;
        IsReclaimable = isReclaimable;
    }

    /// <inheritdoc />
    public bool IsTerminal { get; }

    /// <inheritdoc />
    public bool IsReclaimable { get; }
}
