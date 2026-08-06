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
/// Error handling mode that triggers compensation logic (saga pattern).
/// </summary>
/// <remarks>
/// Use this mode when failures require undoing previously completed operations.
/// When an error occurs, this mode stops execution and triggers compensation
/// for all completed steps/stages in reverse order.
/// Useful for distributed transactions and multi-step operations requiring atomicity.
/// </remarks>
[TypeOption(typeof(ErrorHandlingModesCollection), "Compensate", RestrictToCurrentCompilation = true)]
public sealed class CompensateMode : ErrorHandlingModeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CompensateMode"/> class.
    /// </summary>
    public CompensateMode()
        : base(
            id: 5,
            name: "Compensate",
            continuesExecution: false,  // Stops to execute compensation
            supportsRetry: false,
            triggersCompensation: true)  // Unique to this mode
    {
    }

    /// <inheritdoc/>
    public override Task<IGenericResult> HandleError(
        Exception error,
        IOrchestrationStepExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IGenericResult>(
            GenericResult.Failure(ErrorHandlingLogger.StepFailedTriggeringCompensation(
                NullLogger.Instance, context.StepId, error.Message)));
    }
}
