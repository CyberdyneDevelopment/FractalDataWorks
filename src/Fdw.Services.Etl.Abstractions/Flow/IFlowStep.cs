using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Etl.Abstractions.Flow;

/// <summary>
/// One step of a pipeline's control flow: a stage of components, a loop, a variable set or another pipeline started (FDW-1403).
/// </summary>
/// <remarks>
/// Steps run one after another; the next step starts only when the one before has finished. That is the barrier between
/// stages. Components inside a stage run together, as a flow graph.
/// </remarks>
public interface IFlowStep
{
    /// <summary>Gets the step's name.</summary>
    string Name { get; }

    /// <summary>Runs the step to its end.</summary>
    /// <param name="context">The variables and executor the step runs against.</param>
    /// <param name="cancellationToken">Cancels the step.</param>
    /// <returns>Success, or the failure that stops the pipeline.</returns>
    Task<IGenericResult> Run(FlowStepContext context, CancellationToken cancellationToken = default);
}
