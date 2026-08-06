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
/// Error handling mode that skips failed items and continues execution.
/// </summary>
/// <remarks>
/// Use this mode when individual item failures should not stop the overall process.
/// Failed records are captured for later analysis but execution continues.
/// </remarks>
[TypeOption(typeof(ErrorHandlingModesCollection), "SkipAndContinue", RestrictToCurrentCompilation = true)]
public sealed class SkipAndContinueMode : ErrorHandlingModeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SkipAndContinueMode"/> class.
    /// </summary>
    public SkipAndContinueMode()
        : base(
            id: 2,
            name: "SkipAndContinue",
            continuesExecution: true,
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
        ErrorHandlingLogger.StepFailedSkipAndContinue(
            NullLogger.Instance, context.StepId, error.Message);
        return Task.FromResult<IGenericResult>(GenericResult.Success());
    }
}
