using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Etl.Abstractions.Flow;

/// <summary>
/// Runs a flow graph: validates it, starts every component and moves row batches between them (FDW-1386).
/// </summary>
public interface IFlowExecutor
{
    /// <summary>Runs the graph to completion.</summary>
    /// <param name="graph">The components and links.</param>
    /// <param name="options">How the run buffers rows.</param>
    /// <param name="cancellationToken">Cancels the run.</param>
    /// <returns>The report of what moved, or the first failure; a failure in one component stops the others.</returns>
    Task<IGenericResult<FlowRunReport>> Run(FlowGraph graph, FlowRunOptions options, CancellationToken cancellationToken = default);

    /// <summary>Runs the graph to completion and returns how it ended together with what it did, whether it succeeded or failed (FDW-1691).</summary>
    /// <param name="graph">The components and links.</param>
    /// <param name="options">How the run buffers rows.</param>
    /// <param name="cancellationToken">Cancels the run.</param>
    /// <returns>The outcome and the report; the report names the component whose failure stopped a failed run.</returns>
    Task<FlowRunOutcome> Execute(FlowGraph graph, FlowRunOptions options, CancellationToken cancellationToken = default);
}
