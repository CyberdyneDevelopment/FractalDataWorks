using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;

namespace Fdw.DevSession.Abstractions;

/// <summary>
/// CRTP base class for <see cref="IStrandState"/> options. Each concrete state supplies its id, name, and
/// whether it is terminal.
/// </summary>
[ExcludeFromCodeCoverage]
public abstract class StrandStateBase : TypeOptionBase<int, StrandStateBase>, IStrandState
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StrandStateBase"/> class.
    /// </summary>
    /// <param name="id">The unique identifier.</param>
    /// <param name="name">The state name.</param>
    /// <param name="isTerminal">Whether this is a terminal state.</param>
    protected StrandStateBase(int id, string name, bool isTerminal)
        : base(id, name)
    {
        IsTerminal = isTerminal;
    }

    /// <inheritdoc />
    public bool IsTerminal { get; }
}
