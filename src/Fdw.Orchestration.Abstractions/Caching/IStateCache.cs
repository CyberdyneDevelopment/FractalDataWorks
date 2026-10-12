using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Fdw.Orchestration.Abstractions.Caching;

/// <summary>
/// Cache for orchestration execution state.
/// </summary>
/// <remarks>
/// Persists execution state to enable:
/// - Resuming paused executions
/// - Recovering from failures
/// - Sharing state across distributed executors
/// </remarks>
public interface IStateCache
{
    /// <summary>
    /// Saves the current execution state.
    /// </summary>
    /// <param name="executionId">The execution ID.</param>
    /// <param name="state">The state to save.</param>
    /// <param name="options">Cache options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SaveState(
        string executionId,
        ExecutionState state,
        CacheEntryOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads execution state.
    /// </summary>
    /// <param name="executionId">The execution ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The execution state, or null if not found.</returns>
    Task<ExecutionState?> LoadState(
        string executionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes execution state.
    /// </summary>
    /// <param name="executionId">The execution ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeleteState(
        string executionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active execution IDs for an orchestration.
    /// </summary>
    /// <param name="orchestrationId">The orchestration ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of active execution IDs.</returns>
    Task<IReadOnlyList<string>> GetActiveExecutions(
        string orchestrationId,
        CancellationToken cancellationToken = default);
}