using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Operations.Abstractions.Escalation;

/// <summary>
/// The alerts escalation policies walk (FDW-3863): a threshold that fires opens one on the policy it names, <see cref="Advance"/> sends the levels as they fall due, and a person
/// acknowledges or resolves it. This is the one entry for every kind of threshold (a wait threshold, a pipeline's failure threshold, a component's input and output thresholds).
/// </summary>
/// <remarks>
/// Why a contract of its own and not <see cref="IEscalationEvaluator"/>: that one answers for an execution item, and a threshold is not one. Every method fails loud: a policy that
/// does not exist, is not enabled or has no level is a failure naming it, never an alert that silently sends nothing.
/// </remarks>
public interface IEscalationAlerts
{
    /// <summary>Opens an alert for a threshold that fired.</summary>
    /// <param name="request">The threshold and the policy it names.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>Whether an alert was opened, was already open, or the threshold is cooling down; a failure when the policy is missing, not enabled or has no level.</returns>
    Task<IGenericResult<EscalationOpened>> Open(EscalationAlertRequest request, CancellationToken cancellationToken = default);

    /// <summary>Sends every level that has fallen due, for every open alert, at <paramref name="now"/>.</summary>
    /// <param name="now">The moment judged.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>How many levels were sent; a failure when the alerts cannot be read.</returns>
    Task<IGenericResult<int>> Advance(DateTimeOffset now, CancellationToken cancellationToken = default);

    /// <summary>A person takes an alert: no further level is sent.</summary>
    /// <param name="alertId">The alert.</param>
    /// <param name="acknowledgedBy">Who takes it.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The alert; a failure when it is not found or is not firing.</returns>
    Task<IGenericResult<EscalationAlert>> Acknowledge(Guid alertId, string acknowledgedBy, CancellationToken cancellationToken = default);

    /// <summary>Closes an alert; the policy's cooldown counts from now.</summary>
    /// <param name="alertId">The alert.</param>
    /// <param name="resolvedBy">Who resolves it.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The alert; a failure when it is not found or is already resolved.</returns>
    Task<IGenericResult<EscalationAlert>> Resolve(Guid alertId, string resolvedBy, CancellationToken cancellationToken = default);

    /// <summary>Lists the alerts, newest first.</summary>
    /// <param name="state">One of <see cref="EscalationAlertStates"/> to list only those; none lists every alert.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The alerts the caller's tenant may see.</returns>
    Task<IGenericResult<IReadOnlyList<EscalationAlert>>> List(string? state = null, CancellationToken cancellationToken = default);
}
