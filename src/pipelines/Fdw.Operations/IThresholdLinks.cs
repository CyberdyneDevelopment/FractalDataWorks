using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Operations.Configuration;
using Fdw.Results;

namespace Fdw.Operations;

/// <summary>Attaches thresholds to subjects and detaches them (FDW-3948), through the link table of the subject's kind.</summary>
public interface IThresholdLinks
{
    /// <summary>Attaches a threshold to a subject.</summary>
    /// <param name="subjectKind">A name of ThresholdSubjectKinds; it must be the kind the threshold judges.</param>
    /// <param name="subjectId">The subject's Id in its domain table.</param>
    /// <param name="thresholdName">The threshold's name.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>Success; a failure when the threshold does not exist, judges another kind, or is already attached.</returns>
    Task<IGenericResult> Attach(string subjectKind, Guid subjectId, string thresholdName, CancellationToken cancellationToken = default);

    /// <summary>Detaches a threshold from a subject: the link is retired, never edited.</summary>
    /// <param name="subjectKind">A name of ThresholdSubjectKinds.</param>
    /// <param name="subjectId">The subject's Id in its domain table.</param>
    /// <param name="thresholdName">The threshold's name.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>Success; a failure when the threshold does not exist or is not attached.</returns>
    Task<IGenericResult> Detach(string subjectKind, Guid subjectId, string thresholdName, CancellationToken cancellationToken = default);

    /// <summary>Reads the thresholds attached to a subject.</summary>
    /// <param name="subjectKind">A name of ThresholdSubjectKinds.</param>
    /// <param name="subjectId">The subject's Id in its domain table.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The thresholds, enabled or not; none when none is attached.</returns>
    Task<IGenericResult<IReadOnlyList<IThresholdImplementationConfiguration>>> ThresholdsOf(string subjectKind, Guid subjectId, CancellationToken cancellationToken = default);
}
