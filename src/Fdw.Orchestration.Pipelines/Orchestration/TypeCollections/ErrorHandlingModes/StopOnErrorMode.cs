using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Orchestration.Abstractions;
using Fdw.Orchestration.Abstractions.TypeCollections.ErrorHandlingModeOptions;
using Fdw.Results;
using Fdw.Orchestration.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ErrorHandlingModesCollection = Fdw.Orchestration.Abstractions.TypeCollections.ErrorHandlingModeOptions.ErrorHandlingModes;

namespace Fdw.Orchestration.TypeCollections.ErrorHandlingModes;

/// <summary>
/// Error handling mode that stops execution immediately when an error occurs.
/// </summary>
/// <remarks>
/// Use this mode when any error should halt the entire orchestration.
/// No retry attempts are made and the error is propagated immediately.
/// </remarks>
[TypeOption(typeof(ErrorHandlingModesCollection), "StopOnError", RestrictToCurrentCompilation = true)]
public sealed class StopOnErrorMode : ErrorHandlingModeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StopOnErrorMode"/> class.
    /// </summary>
    public StopOnErrorMode()
        : base(
            id: 1,
            name: "StopOnError",
            continuesExecution: false,
            supportsRetry: false,
            triggersCompensation: false)
    {
    }

    /// <inheritdoc/>
    public override Task<IGenericResult> HandleError(
        Exception error,
        IOrchestrationStepExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IGenericResult>(
            GenericResult.Failure(ErrorHandlingLogger.StepFailedStopOnError(
                NullLogger.Instance, context.StepId, error.Message)));
    }
}
