using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections;
using Fdw.Results;

namespace Fdw.Orchestration.Abstractions.TypeCollections.ErrorHandlingModeOptions;

/// <summary>
/// Base class for error handling mode TypeOptions.
/// </summary>
/// <remarks>
/// Provides common functionality for error handling modes used in orchestration steps.
/// Derived classes implement specific error handling behaviors.
/// </remarks>
public abstract class ErrorHandlingModeBase : TypeOptionBase<int, ErrorHandlingModeBase>, IErrorHandlingMode
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ErrorHandlingModeBase"/> class.
    /// </summary>
    /// <param name="id">Unique numeric identifier.</param>
    /// <param name="name">Human-readable name.</param>
    /// <param name="continuesExecution">Whether execution continues after error.</param>
    /// <param name="supportsRetry">Whether this mode supports retry attempts.</param>
    /// <param name="triggersCompensation">Whether this mode triggers compensation logic.</param>
    protected ErrorHandlingModeBase(
        int id,
        string name,
        bool continuesExecution,
        bool supportsRetry,
        bool triggersCompensation)
        : base(id, name, $"ErrorHandlingModes:{name}", name, $"{name} error handling", "Orchestration")
    {
        ContinuesExecution = continuesExecution;
        SupportsRetry = supportsRetry;
        TriggersCompensation = triggersCompensation;
    }

    /// <inheritdoc/>
    public bool ContinuesExecution { get; }

    /// <inheritdoc/>
    public bool SupportsRetry { get; }

    /// <inheritdoc/>
    public bool TriggersCompensation { get; }

    /// <inheritdoc/>
    public abstract Task<IGenericResult> HandleError(
        Exception error,
        IOrchestrationStepExecutionContext context,
        CancellationToken cancellationToken = default);
}
