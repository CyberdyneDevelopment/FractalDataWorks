using System.Threading;
using System.Threading.Tasks;
using Fdw.Services.Etl.Abstractions.Execution;

namespace Fdw.Services.Etl.Execution;

/// <summary>
/// Runs one stored pipeline request through the engine (FDW-6631): the code both the queued executor and the AtOnce runner call, so a run that waits for its turn and a run
/// that starts at once are run by the same code. Internal, because the refusal a run is closed with before it runs is an internal type.
/// </summary>
internal interface IPipelineRequestProcessor
{
    /// <summary>Runs the request and closes its record on every way a run can end.</summary>
    /// <param name="request">The request.</param>
    /// <param name="refusal">Why the run is refused before it runs, or null for a run that runs.</param>
    /// <param name="run">The run's handle, which a stop reaches.</param>
    /// <param name="stoppingToken">The host's stop token for the run.</param>
    /// <returns>A task that ends when the run has ended and its record is closed.</returns>
    Task ProcessRequest(PipelineExecutionRequest request, PipelineRunRefusal? refusal, PipelineRunHandle run, CancellationToken stoppingToken);
}
