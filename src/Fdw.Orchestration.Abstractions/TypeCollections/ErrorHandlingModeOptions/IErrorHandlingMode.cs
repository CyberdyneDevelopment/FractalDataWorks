using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections;
using Fdw.Results;

namespace Fdw.Orchestration.Abstractions.TypeCollections.ErrorHandlingModeOptions;

/// <summary>
/// Interface for error handling mode TypeOptions.
/// </summary>
/// <remarks>
/// Error handling modes define how an orchestration step responds to errors.
/// Options include stopping execution, skipping and continuing, retrying,
/// or redirecting failed items to a dead letter destination.
/// </remarks>
public interface IErrorHandlingMode : ITypeOption<int, ErrorHandlingModeBase>
{
    /// <summary>
    /// Gets whether execution continues after an error occurs.
    /// </summary>
    bool ContinuesExecution { get; }


    /// <summary>
    /// Gets whether this mode supports retry attempts.
    /// </summary>
    bool SupportsRetry { get; }

    /// <summary>
    /// Gets whether this mode triggers compensation logic (saga pattern).
    /// </summary>
    /// <remarks>
    /// When true, the orchestration will execute compensation for all completed phases
    /// in reverse order to rollback changes.
    /// </remarks>
    bool TriggersCompensation { get; }

    /// <summary>
    /// Handles an error that occurred during step execution.
    /// </summary>
    /// <param name="error">The error that occurred.</param>
    /// <param name="context">Execution context information.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result indicating how the error was handled.</returns>
    Task<IGenericResult> HandleError(
        Exception error,
        IOrchestrationStepExecutionContext context,
        CancellationToken cancellationToken = default);
}
