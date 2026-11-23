using Fdw.Results;
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Etl.Logging;
using Fdw.Services.Pipelines.Abstractions.Runs;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Execution;

/// <summary>
/// The step limit of one run (FDW-6141): takes each step from the run's <see cref="PipelineRunTimeAndStepLimits"/> before it runs, and refuses the first step the limits do not
/// allow with a coded failure that names the pipeline, the step and what is left.
/// </summary>
public sealed class PipelineRunStepLimit : IFlowStepLimit
{
    private readonly PipelineRunTimeAndStepLimits _limits;
    private readonly string _pipelineName;
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="PipelineRunStepLimit"/> class.</summary>
    /// <param name="limits">The run's limits, which count its steps and every parent's above it.</param>
    /// <param name="pipelineName">The pipeline the run is of, which a refusal names.</param>
    /// <param name="logger">The logger a refusal is written to.</param>
    public PipelineRunStepLimit(PipelineRunTimeAndStepLimits limits, string pipelineName, ILogger logger)
    {
        _limits = limits;
        _pipelineName = pipelineName;
        _logger = logger;
    }

    /// <inheritdoc />
    public IGenericResult TakeStep(string stepName) =>
        _limits.TryTakeStep()
            ? GenericResult.Success()
            : GenericResult.Failure(PipelineRunLog.StepLimitReached(_logger, _pipelineName, stepName));
}
