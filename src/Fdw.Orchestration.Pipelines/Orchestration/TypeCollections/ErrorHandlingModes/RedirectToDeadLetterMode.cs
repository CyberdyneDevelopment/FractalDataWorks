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
/// Error handling mode that redirects failed items to a dead letter destination and continues.
/// </summary>
/// <remarks>
/// Use this mode when failures need to be captured for later reprocessing while allowing
/// the main process to continue. Typical destinations include dead letter queues,
/// error tables, or designated storage locations.
/// </remarks>
[TypeOption(typeof(ErrorHandlingModesCollection), "RedirectToDeadLetter", RestrictToCurrentCompilation = true)]
public sealed class RedirectToDeadLetterMode : ErrorHandlingModeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RedirectToDeadLetterMode"/> class.
    /// </summary>
    public RedirectToDeadLetterMode()
        : base(
            id: 4,
            name: "RedirectToDeadLetter",
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
        ErrorHandlingLogger.RedirectedToDeadLetter(
            NullLogger.Instance, context.StepId, error.Message);
        return Task.FromResult<IGenericResult>(GenericResult.Success());
    }
}
