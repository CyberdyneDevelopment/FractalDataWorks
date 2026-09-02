using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Fdw.Abstractions;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions;
using Fdw.Services.Etl.Abstractions.Execution;
using Fdw.Services.Etl.Logging;

namespace Fdw.Services.Etl.Pipelines;

/// <summary>
/// Base class for pipelines that provides IGenericService implementation.
/// </summary>
public abstract class EtlPipelineBase : IEtlPipeline
{
    private readonly ILogger<EtlPipelineBase> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="EtlPipelineBase"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    protected EtlPipelineBase(ILogger<EtlPipelineBase>? logger = null)
    {
        _logger = logger ?? NullLogger<EtlPipelineBase>.Instance;
    }

    /// <inheritdoc />
    public abstract Guid Id { get; }

    /// <inheritdoc />
    public abstract string Name { get; }

    /// <inheritdoc />
    public abstract string PipelineType { get; }

    /// <inheritdoc />
    public abstract bool IsExecuting { get; }

    /// <inheritdoc />
    public abstract Task<IGenericResult<IEtlPipelineExecutionResult>> Execute(CancellationToken cancellationToken = default);

    /// <inheritdoc />
    public abstract Task<IGenericResult<IEtlPipelineExecutionResult>> Execute(PipelineExecutionOptions options, CancellationToken cancellationToken = default);

    /// <inheritdoc />
    public abstract IGenericResult Validate();

    #region IGenericService Explicit Implementation

    /// <inheritdoc />
    string IPlatformService.Id => Id.ToString();

    /// <inheritdoc />
    string IPlatformService.ServiceType => PipelineType;

    /// <inheritdoc />
    bool IPlatformService.IsAvailable => !IsExecuting;

    /// <inheritdoc />
    Task<IGenericResult<T>> IGenericService.Execute<T>(IGenericCommand command, CancellationToken cancellationToken)
    {
        return Task.FromResult(GenericResult<T>.Failure(
            EtlLog.CommandExecutionNotSupported(_logger, Name)));
    }

    /// <inheritdoc />
    Task<IGenericResult> IGenericService.Execute(IGenericCommand command, CancellationToken cancellationToken)
    {
        return Task.FromResult((IGenericResult)GenericResult.Failure(
            EtlLog.CommandExecutionNotSupported(_logger, Name)));
    }

    #endregion

    #region IDisposable Implementation

    /// <summary>
    /// Releases resources used by the pipeline.
    /// </summary>
    public virtual void Dispose()
    {
        // Default implementation - derived classes can override to dispose resources
        GC.SuppressFinalize(this);
    }

    #endregion
}
