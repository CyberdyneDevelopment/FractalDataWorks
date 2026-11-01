using System.Threading;

namespace Fdw.StateCollections;

/// <summary>
/// Builds a per-entity <see cref="IStateMachine{TState}"/>. Implementations hold the shared
/// configuration (state graph, handler chain) and wire it against a specific entity's
/// context. Registered through ServiceTypeCollection three-phase DI; never resolved from
/// the container at the use site (see FDW no-service-locator rule).
/// </summary>
/// <typeparam name="TState">The state interface for this machine's domain.</typeparam>
/// <typeparam name="TEntity">The entity type the machine is bound to.</typeparam>
public interface IStateMachineFactory<TState, TEntity>
    where TState : IStateOption<TState>
{
    /// <summary>Build a machine bound to one entity instance.</summary>
    IStateMachine<TState> Build(TEntity entity, CancellationToken cancellationToken);
}
