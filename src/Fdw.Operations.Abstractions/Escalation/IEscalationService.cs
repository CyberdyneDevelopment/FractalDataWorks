using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Operations.Abstractions.Escalation;

/// <summary>
/// Service for managing escalation policies.
/// </summary>
public interface IEscalationService
{
    /// <summary>
    /// Gets an escalation policy by ID.
    /// </summary>
    /// <param name="policyId">The policy ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The escalation policy.</returns>
    Task<IGenericResult<IEscalationPolicy>> GetPolicy(
        Guid policyId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all escalation policies.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of all escalation policies.</returns>
    Task<IGenericResult<IReadOnlyList<IEscalationPolicy>>> GetAllPolicies(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the applicable escalation policy for a workflow.
    /// </summary>
    /// <param name="workflowId">The workflow ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The escalation policy, or null if none applies.</returns>
    Task<IGenericResult<IEscalationPolicy?>> GetPolicyForWorkflow(
        Guid workflowId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the applicable escalation policy for a schedule.
    /// </summary>
    /// <param name="scheduleId">The schedule ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The escalation policy, or null if none applies.</returns>
    Task<IGenericResult<IEscalationPolicy?>> GetPolicyForSchedule(
        Guid scheduleId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new escalation policy.
    /// </summary>
    /// <param name="policy">The policy to create.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created escalation policy.</returns>
    Task<IGenericResult<IEscalationPolicy>> CreatePolicy(
        IEscalationPolicy policy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing escalation policy.
    /// </summary>
    /// <param name="policyId">The policy ID to update.</param>
    /// <param name="policy">The updated policy.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated escalation policy.</returns>
    Task<IGenericResult<IEscalationPolicy>> UpdatePolicy(
        Guid policyId,
        IEscalationPolicy policy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an escalation policy.
    /// </summary>
    /// <param name="policyId">The policy ID to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<IGenericResult> DeletePolicy(
        Guid policyId,
        CancellationToken cancellationToken = default);
}
