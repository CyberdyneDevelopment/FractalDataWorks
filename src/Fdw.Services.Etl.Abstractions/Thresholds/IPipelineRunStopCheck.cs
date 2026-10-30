using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions.Execution;

namespace Fdw.Services.Etl.Abstractions.Thresholds;

/// <summary>
/// Says whether a run that arrives is taken (FDW-3161). A threshold on waiting runs can stop new runs while it fires; a run it stops is not queued, and its caller is
/// told the server is busy and why.
/// </summary>
public interface IPipelineRunStopCheck
{
    /// <summary>Checks a run that arrives against the thresholds that apply to its schedule and its pipeline.</summary>
    /// <param name="request">The run that arrives.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Whether the run is stopped and by which threshold; a failure naming the configuration that cannot be read or is stated wrongly.</returns>
    Task<IGenericResult<PipelineRunStopCheck>> Check(PipelineExecutionRequest request, CancellationToken cancellationToken = default);
}
