using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Agents.Abstractions;

/// <summary>
/// Service for managing the AI agent action review queue.
/// </summary>
public interface IAgentActionService
{
    /// <summary>
    /// Lists agent actions with optional status filter.
    /// </summary>
    /// <param name="status">Optional status filter (Pending, Approved, Denied).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result containing the list of agent actions.</returns>
    Task<IGenericResult<IReadOnlyList<AgentActionRecord>>> List(
        string? status = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a single agent action by ID.
    /// </summary>
    /// <param name="actionId">The action identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result containing the agent action, or failure if not found.</returns>
    Task<IGenericResult<AgentActionRecord>> Get(
        int actionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Approves a pending agent action.
    /// </summary>
    /// <param name="actionId">The action identifier.</param>
    /// <param name="reviewedBy">The user ID of the reviewer.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<IGenericResult> Approve(
        int actionId,
        string reviewedBy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Denies a pending agent action.
    /// </summary>
    /// <param name="actionId">The action identifier.</param>
    /// <param name="reviewedBy">The user ID of the reviewer.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<IGenericResult> Deny(
        int actionId,
        string reviewedBy,
        CancellationToken cancellationToken = default);
}
