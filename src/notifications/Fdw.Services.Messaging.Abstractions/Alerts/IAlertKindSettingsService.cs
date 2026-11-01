using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Messaging;

/// <summary>
/// Reads and changes which actions each kind of alert offers the people of the caller's company (FDW-2430). The company is the caller's active tenant; who may
/// change it is the route's right, and this service asks nothing more.
/// </summary>
public interface IAlertKindSettingsService
{
    /// <summary>
    /// Lists every kind of alert with its default actions and the actions it offers the caller's company.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The kinds, by name.</returns>
    Task<IGenericResult<IReadOnlyList<AlertKindSummary>>> List(CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the actions a kind of alert offers the caller's company, replacing its choice whole. An empty list is a choice: the kind then offers nothing.
    /// </summary>
    /// <param name="kind">The kind of alert.</param>
    /// <param name="actions">The names of the actions to offer, in order; each an option of <see cref="AlertActions"/> and none repeated. Required: a request that sends none is refused.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The kind as it now stands.</returns>
    Task<IGenericResult<AlertKindSummary>> Save(string kind, IReadOnlyList<string>? actions, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes the caller's company's choice for a kind of alert, so the kind's own list applies again.
    /// </summary>
    /// <param name="kind">The kind of alert.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The kind as it now stands.</returns>
    Task<IGenericResult<AlertKindSummary>> Reset(string kind, CancellationToken cancellationToken = default);
}
