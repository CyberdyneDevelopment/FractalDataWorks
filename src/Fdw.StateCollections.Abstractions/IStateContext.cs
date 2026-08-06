using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.StateCollections;

/// <summary>
/// Per-entity execution context handed to <see cref="IStateOption{TSelf}.OnEnter"/> and
/// <see cref="IStateOption{TSelf}.OnExit"/>. Composes — never redeclares — the cross-cutting
/// fields an execution context already carries (correlation id, services, cancellation).
/// Domains extend this with their own context interfaces when they need entity references
/// or domain-specific telemetry.
/// </summary>
/// <typeparam name="TState">The state interface for this machine's domain.</typeparam>
public interface IStateContext<TState>
    where TState : IStateOption<TState>
{
    /// <summary>The state the engine currently believes the entity is in, prior to the requested transition.</summary>
    TState CurrentState { get; }

    /// <summary>The correlation id used in transition records + audit entries.</summary>
    Guid CorrelationId { get; }

    /// <summary>Service provider scoped to this transition; OnEnter / OnExit pull domain services here.</summary>
    IServiceProvider Services { get; }

    /// <summary>
    /// Transition handlers to invoke after a successful transition. Resolved from DI as
    /// <c>IEnumerable&lt;IStateTransitionHandler&lt;TState&gt;&gt;</c>; called in registration order.
    /// </summary>
    IReadOnlyList<IStateTransitionHandler<TState>> TransitionHandlers { get; }

    /// <summary>
    /// Persist a state change for the bound entity. Implementations route through DataGateway
    /// or whatever durable backing the entity uses. Returns failure if the write fails;
    /// the engine treats the transition as aborted on failure.
    /// </summary>
    Task<IGenericResult> PersistState(TState target, CancellationToken cancellationToken);
}
