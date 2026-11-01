using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.StateCollections;

/// <summary>
/// Per-entity facade an engine exposes to callers. Wraps the smart-state graph + persistence
/// hook + handler chain for one specific entity. Construct via
/// <see cref="IStateMachineFactory{TState, TEntity}.Build"/>; never resolve directly from DI.
/// </summary>
/// <typeparam name="TState">The state interface for this machine's domain.</typeparam>
public interface IStateMachine<TState>
    where TState : IStateOption<TState>
{
    /// <summary>The entity's current state, mirroring the persisted value.</summary>
    TState CurrentState { get; }

    /// <summary>
    /// True when <paramref name="target"/> is reachable from <see cref="CurrentState"/> via
    /// the smart-state graph. Cheaper than Fire — useful for UI affordance gating.
    /// </summary>
    bool CanProgressTo(TState target);

    /// <summary>
    /// Validate and apply a transition to <paramref name="target"/>. Returns failure when:
    /// the target is not in <see cref="IStateOption{TSelf}.CanProgressTo"/>; OnExit fails;
    /// persistence fails; OnEnter fails. Never throws for "expected" failure modes.
    /// </summary>
    Task<IGenericResult> ProgressTo(TState target, CancellationToken cancellationToken = default);
}
