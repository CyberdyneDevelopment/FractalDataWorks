using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Messaging;

/// <summary>
/// One kind of alert (FDW-2420): UI comments, messages. It says which actions its alerts offer, finds an alert for the caller, and is told when a person acts.
/// A developer adds a kind by registering a source of their own.
/// </summary>
public interface IAlertSource
{
    /// <summary>Gets the name of the kind, which a caller sends and every action on its alerts is recorded under.</summary>
    string Kind { get; }

    /// <summary>Gets the names of the actions this kind offers, in the order a menu shows them. Each is an option of <see cref="AlertActions"/>.</summary>
    IReadOnlyList<string> OfferedActions { get; }

    /// <summary>Finds an alert for the caller.</summary>
    /// <param name="id">The alert's identifier.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The alert, or success with no value when it does not exist or the caller may not see it; a failure only when the source could not look.</returns>
    Task<IGenericResult<AlertSubject?>> Find(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Tells the source that the caller took an action on one of its alerts, after it was recorded.</summary>
    /// <param name="id">The alert's identifier.</param>
    /// <param name="action">The action taken.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>Success when the source has done what it does in answer.</returns>
    Task<IGenericResult> Acted(Guid id, IAlertAction action, CancellationToken cancellationToken = default);
}
