using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Operations.Data;
using Fdw.Results;

namespace Fdw.Operations.Escalation;

/// <summary>Where the alerts of escalation policies are kept (FDW-3863): the rows of ops.EscalationAlert.</summary>
internal interface IEscalationAlertStore
{
    /// <summary>Adds a new alert.</summary>
    /// <param name="alert">The alert.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>A failure naming why when the alert could not be kept.</returns>
    Task<IGenericResult> Add(EscalationAlertRow alert, CancellationToken cancellationToken = default);

    /// <summary>Writes the changed state of an alert that is kept.</summary>
    /// <param name="alert">The alert, with its id.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>A failure naming why when the alert could not be written.</returns>
    Task<IGenericResult> Update(EscalationAlertRow alert, CancellationToken cancellationToken = default);

    /// <summary>Finds an alert by its id.</summary>
    /// <param name="id">The alert's id.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The alert; none when there is none with that id.</returns>
    Task<IGenericResult<EscalationAlertRow?>> Find(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Lists every alert of a threshold, newest first.</summary>
    /// <param name="tenantId">The threshold's tenant.</param>
    /// <param name="subjectKind">The owner's kind.</param>
    /// <param name="subjectName">The owner's name.</param>
    /// <param name="thresholdName">The threshold's name.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The threshold's alerts, open and closed.</returns>
    Task<IGenericResult<IReadOnlyList<EscalationAlertRow>>> OfThreshold(
        Guid tenantId, string subjectKind, string subjectName, string thresholdName, CancellationToken cancellationToken = default);

    /// <summary>Lists alerts, newest first.</summary>
    /// <param name="state">One state to list; none lists every alert.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The alerts, whatever their tenant: the caller of the store narrows them.</returns>
    Task<IGenericResult<IReadOnlyList<EscalationAlertRow>>> List(string? state, CancellationToken cancellationToken = default);
}
