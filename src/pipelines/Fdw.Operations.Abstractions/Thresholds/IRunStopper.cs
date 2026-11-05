using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Operations.Abstractions.Thresholds;

/// <summary>Stops a run that is in flight, for a threshold consequence (FDW-3936). The host that runs pipelines registers it.</summary>
public interface IRunStopper
{
    /// <summary>Stops a run; its records end Cancelled with the reason.</summary>
    /// <param name="runId">The run.</param>
    /// <param name="reason">Why it is stopped; the run message carries it.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>Success, or a failure when the run is not held by this host.</returns>
    Task<IGenericResult> Stop(Guid runId, string reason, CancellationToken cancellationToken = default);
}
