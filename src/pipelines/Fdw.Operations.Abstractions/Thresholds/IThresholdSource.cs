using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Operations.Abstractions.Thresholds;

/// <summary>Finds the thresholds attached to a subject (FDW-3936), through the threshold configuration provider and the subject's link table.</summary>
public interface IThresholdSource
{
    /// <summary>Loads the thresholds attached to a subject, in the scope of its tenant.</summary>
    /// <param name="subject">The subject.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The thresholds, enabled or not; none when none is attached. A failure when they cannot be read.</returns>
    Task<IGenericResult<IReadOnlyList<IThresholdDefinition>>> For(ThresholdSubject subject, CancellationToken cancellationToken = default);
}
