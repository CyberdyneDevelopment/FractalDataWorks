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
/// Error handling mode that retries failed operations with a configurable backoff strategy.
/// </summary>
/// <remarks>
/// Use this mode for transient failures that may succeed on retry.
/// The actual retry execution is handled by Polly via the resilience pipeline.
/// This mode signals intent and provides configuration for the retry behavior.
/// </remarks>
[TypeOption(typeof(ErrorHandlingModesCollection), "RetryWithBackoff", RestrictToCurrentCompilation = true)]
public sealed class RetryWithBackoffMode : ErrorHandlingModeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RetryWithBackoffMode"/> class.
    /// </summary>
    public RetryWithBackoffMode()
        : base(
            id: 3,
            name: "RetryWithBackoff",
            continuesExecution: true,
            supportsRetry: true,
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
            GenericResult.Failure(ErrorHandlingLogger.StepAttemptFailed(
                NullLogger.Instance, context.StepId, context.AttemptNumber, error.Message)));
    }
}
