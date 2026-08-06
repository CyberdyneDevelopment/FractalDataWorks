using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.StateCollections;

/// <summary>
/// Receives a successful transition after the engine has persisted the new state. Domains
/// register one or more handlers — typically a DataGateway-backed audit writer plus any
/// downstream notifiers — and the engine awaits them in registration order. A failing
/// handler does NOT roll back the transition; it only surfaces in the engine's result
/// alongside the transition's success.
/// </summary>
/// <typeparam name="TState">The state interface for this machine's domain.</typeparam>
public interface IStateTransitionHandler<TState>
    where TState : IStateOption<TState>
{
    /// <summary>Process a completed transition.</summary>
    Task<IGenericResult> Handle(IStateTransition<TState> transition, CancellationToken cancellationToken);
}
