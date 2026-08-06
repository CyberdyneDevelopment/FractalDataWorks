using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Operations.Abstractions.Escalation;

/// <summary>
/// Service for evaluating and triggering escalation policies.
/// </summary>
public interface IEscalationEvaluator
{
    /// <summary>
    /// Evaluates whether escalation should be triggered for an execution item.
    /// </summary>
    /// <param name="executionItemId">The execution item ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result indicating whether escalation should be triggered.</returns>
    Task<IGenericResult<bool>> ShouldEscalate(
        Guid executionItemId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Triggers escalation for an execution item.
    /// </summary>
    /// <param name="executionItemId">The execution item ID.</param>
    /// <param name="level">The escalation level to trigger.</param>
    /// <param name="message">Optional message to include in the notification.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<IGenericResult> TriggerEscalation(
        Guid executionItemId,
        int level,
        string? message = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the escalation policy for an execution item.
    /// </summary>
    /// <param name="executionItemId">The execution item ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The escalation policy, or null if none applies.</returns>
    Task<IGenericResult<IEscalationPolicy?>> GetPolicy(
        Guid executionItemId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current escalation level for an execution item.
    /// </summary>
    /// <param name="executionItemId">The execution item ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The current escalation level (0 if not escalated).</returns>
    Task<IGenericResult<int>> GetCurrentLevel(
        Guid executionItemId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resets escalation state for an execution item (e.g., when issue is resolved).
    /// </summary>
    /// <param name="executionItemId">The execution item ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<IGenericResult> ResetEscalation(
        Guid executionItemId,
        CancellationToken cancellationToken = default);
}
