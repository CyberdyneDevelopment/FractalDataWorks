using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections;
using Fdw.Results;

namespace Fdw.StateCollections;

/// <summary>
/// A "smart state" — a <see cref="ITypeOption{TKey, TValue}"/> that owns its own outbound
/// transition table via <see cref="CanProgressTo"/> and its own entry/exit behavior.
/// Each domain defines a derived interface (e.g. <c>IWorkflowState</c>) that adds named
/// transition properties (<c>Next</c>, <c>Abort</c>, <c>Skip</c>, …) returning the appropriate
/// <typeparamref name="TSelf"/>. Smart-state design pressure: invalid transitions are described
/// at the option level, not at a separate transition table.
/// </summary>
/// <typeparam name="TSelf">The concrete state interface (CRTP).</typeparam>
public interface IStateOption<TSelf> : ITypeOption<int, TSelf>
    where TSelf : IStateOption<TSelf>
{
    /// <summary>
    /// Every other state this state is permitted to progress to. Empty for terminal states.
    /// The engine validates the caller's requested target against this collection before
    /// running OnExit / OnEnter.
    /// </summary>
    IReadOnlyList<TSelf> CanProgressTo { get; }

    /// <summary>
    /// True when this state is terminal (no outbound transitions).
    /// </summary>
    bool IsTerminal { get; }

    /// <summary>
    /// Side effects invoked when the engine enters this state. Receives a context that
    /// composes the entity, persistence hook, and transition correlation id. Returning
    /// a failure aborts the transition and leaves the persisted state at the prior value.
    /// </summary>
    Task<IGenericResult> OnEnter(IStateContext<TSelf> context, CancellationToken cancellationToken);

    /// <summary>
    /// Side effects invoked when the engine exits this state. Returning a failure aborts
    /// the transition before any persistence write.
    /// </summary>
    Task<IGenericResult> OnExit(IStateContext<TSelf> context, CancellationToken cancellationToken);
}
