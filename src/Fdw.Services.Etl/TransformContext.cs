using System;
using System.Collections.Generic;
using System.Threading;
using Fdw.Services.Etl.Abstractions;
using Fdw.Services.Etl.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl;

/// <summary>
/// Implementation of <see cref="ITransformContext"/> for ETL transform execution.
/// </summary>
public sealed class TransformContext : ITransformContext
{
    private readonly List<TransformError> _errors = [];
    private static readonly IReadOnlyDictionary<string, object?> EmptyParameters =
        new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Initializes a new instance of the <see cref="TransformContext"/> class.
    /// </summary>
    /// <param name="executionId">The parent pipeline execution ID.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="variables">Pipeline variables.</param>
    /// <param name="calculationEngine">The calculation engine (optional).</param>
    /// <param name="connectionProvider">The connection provider (optional).</param>
    /// <param name="dataGateway">The data gateway for lookups (optional).</param>
    /// <param name="services">Optional service provider.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    public TransformContext(
        Guid executionId,
        ILogger logger,
        IReadOnlyDictionary<string, object?> variables,
        object? calculationEngine = null,
        object? connectionProvider = null,
        object? dataGateway = null,
        IServiceProvider? services = null,
        CancellationToken cancellationToken = default)
    {
        ExecutionId = executionId;
        Logger = logger ?? NullLogger<TransformContext>.Instance;
        Variables = variables;
        CalculationEngine = calculationEngine;
        ConnectionProvider = connectionProvider;
        DataGateway = dataGateway;
        Services = services ?? EmptyServiceProvider.Instance;
        CancellationToken = cancellationToken;
    }

    /// <inheritdoc />
    public Guid ExecutionId { get; }

    /// <inheritdoc />
    public DateTimeOffset StartTime { get; } = DateTimeOffset.UtcNow;

    /// <inheritdoc />
    public CancellationToken CancellationToken { get; }

    /// <inheritdoc />
    public ILogger Logger { get; }

    /// <inheritdoc />
    public IServiceProvider Services { get; }

    /// <inheritdoc />
    // Why: ITransformContext exposes Variables for ETL-specific pipeline variable access.
    // IExecutionContext.Parameters is the universal key/value store — kept empty at this
    // layer since ETL callers pass data via Variables, not Parameters.
    public IReadOnlyDictionary<string, object?> Parameters => EmptyParameters;

    /// <inheritdoc />
    public IDictionary<string, object?> SharedState { get; } =
        new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc />
    public IReadOnlyDictionary<string, object?> Variables { get; }

    /// <inheritdoc />
    public object? CalculationEngine { get; }

    /// <inheritdoc />
    public object? ConnectionProvider { get; }

    /// <inheritdoc />
    public object? DataGateway { get; }

    /// <summary>
    /// Gets the errors reported during transform execution.
    /// </summary>
    public IReadOnlyList<TransformError> Errors => _errors;

    /// <inheritdoc />
    public void ReportError(string error, IDictionary<string, object?>? record = null)
    {
        _errors.Add(new TransformError(error, record));
        EtlLog.RecordProcessingFailed(Logger, ExecutionId.ToString(), error);
    }

    /// <summary>
    /// Minimal service provider returned when no DI container is available.
    /// </summary>
    // Why: TransformContext is constructed by the ETL executor which may not have a
    // full DI container in all scenarios. EmptyServiceProvider prevents null reference
    // exceptions while making the absence of services explicit.
    private sealed class EmptyServiceProvider : IServiceProvider
    {
        /// <summary>Gets the singleton instance.</summary>
        public static readonly EmptyServiceProvider Instance = new();

        /// <inheritdoc/>
        public object? GetService(Type serviceType) => null;
    }

    /// <summary>
    /// Represents a transform error with associated record data.
    /// </summary>
    public sealed class TransformError
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TransformError"/> class.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="record">The record that caused the error.</param>
        public TransformError(string message, IDictionary<string, object?>? record)
        {
            Message = message;
            Record = record;
        }

        /// <summary>
        /// Gets the error message.
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// Gets the record that caused the error.
        /// </summary>
        public IDictionary<string, object?>? Record { get; }
    }
}
