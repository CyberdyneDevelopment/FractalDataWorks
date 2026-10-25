using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Messaging;

/// <summary>
/// Lists what alerts allow the caller and applies one action to an alert (FDW-2420). The caller is the authentication context's: a person acts as themselves,
/// and an alert they may not see does not exist for them.
/// </summary>
public interface IAlertActionService
{
    /// <summary>
    /// Says what each named alert allows the caller and what has been done to it.
    /// </summary>
    /// <param name="alerts">The alerts asked about, at least one and at most <see cref="AlertLimits.MaxAlerts"/>.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The alerts found, and those that do not exist or that the caller may not see.</returns>
    Task<IGenericResult<AlertList>> List(IReadOnlyList<AlertKey> alerts, CancellationToken cancellationToken = default);

    /// <summary>
    /// Takes an action on an alert as the caller. Taking the same action again changes nothing, except that a timed action moves its end.
    /// </summary>
    /// <param name="kind">The kind of alert.</param>
    /// <param name="id">The alert's identifier.</param>
    /// <param name="action">The name of the action.</param>
    /// <param name="choice">The name of the length of time, for an action that offers lengths; otherwise <see langword="null"/>.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The alert as it is now.</returns>
    Task<IGenericResult<AlertSummary>> Apply(string kind, Guid id, string action, string? choice, CancellationToken cancellationToken = default);

    /// <summary>
    /// Takes back one of the caller's own actions on an alert. An action taken for everyone cannot be taken back here.
    /// </summary>
    /// <param name="kind">The kind of alert.</param>
    /// <param name="id">The alert's identifier.</param>
    /// <param name="action">The name of the action.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The alert as it is now.</returns>
    Task<IGenericResult<AlertSummary>> TakeBack(string kind, Guid id, string action, CancellationToken cancellationToken = default);
}
