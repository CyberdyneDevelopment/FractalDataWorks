using System;
using System.Collections.Generic;
using System.Threading;
using Fdw.Services.Calculations.Abstractions.Runtime;
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
    private readonly List<RejectedRecord> _rejections = [];
    private static readonly IReadOnlyDictionary<string, object?> EmptyParameters =
        new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
    private static readonly IReadOnlyDictionary<Guid, ICompiledCalculation> NoBuiltCalculations =
        new Dictionary<Guid, ICompiledCalculation>();
    private static readonly IReadOnlyDictionary<Guid, ICompiledCalculationSet> NoBuiltSets =
        new Dictionary<Guid, ICompiledCalculationSet>();

    /// <summary>
    /// Initializes a new instance of the <see cref="TransformContext"/> class.
    /// </summary>
    /// <param name="executionId">The parent pipeline execution ID.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="variables">Pipeline variables.</param>
    /// <param name="fieldTransformChains">The transformer chains the run resolved for the field mappings of its Map stages, by mapping id. The caller states them: a run whose mappings state no chain passes an empty map, so what the run resolved is never assumed here.</param>
    /// <param name="calculationEngine">The calculation engine (optional).</param>
    /// <param name="connectionProvider">The connection provider (optional).</param>
    /// <param name="dataGateway">The data gateway for lookups (optional).</param>
    /// <param name="services">Optional service provider.</param>
    /// <param name="parameters">The run's typed parameter values by name, including the last-successful watermark when there is one; none when the run has none.</param>
    /// <param name="builtCalculations">The calculations the run compiled for its Compute stages, by stage id; none when it has no Compute stage.</param>
    /// <param name="builtSets">The calculation sets the run compiled for its Compute stages that read and write tables, by stage id; none when it has none.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    public TransformContext(
        Guid executionId,
        ILogger logger,
        IReadOnlyDictionary<string, object?> variables,
        IReadOnlyDictionary<Guid, IReadOnlyList<IFieldMappingTransform>> fieldTransformChains,
        object? calculationEngine = null,
        object? connectionProvider = null,
        object? dataGateway = null,
        IServiceProvider? services = null,
        IReadOnlyDictionary<string, object?>? parameters = null,
        IReadOnlyDictionary<Guid, ICompiledCalculation>? builtCalculations = null,
        IReadOnlyDictionary<Guid, ICompiledCalculationSet>? builtSets = null,
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
        Parameters = parameters is null ? EmptyParameters : parameters;
        FieldTransformChains = fieldTransformChains;
        BuiltCalculations = builtCalculations is null ? NoBuiltCalculations : builtCalculations;
        BuiltSets = builtSets is null ? NoBuiltSets : builtSets;
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
    public IReadOnlyDictionary<string, object?> Parameters { get; }

    /// <inheritdoc />
    public IReadOnlyDictionary<Guid, IReadOnlyList<IFieldMappingTransform>> FieldTransformChains { get; }

    /// <inheritdoc />
    public IReadOnlyDictionary<Guid, ICompiledCalculation> BuiltCalculations { get; }

    /// <inheritdoc />
    public IReadOnlyDictionary<Guid, ICompiledCalculationSet> BuiltSets { get; }

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
    /// Gets the records the stages rejected during transform execution, in the order they were rejected.
    /// </summary>
    public IReadOnlyList<RejectedRecord> Rejections => _rejections;

    /// <inheritdoc />
    public void Reject(string stage, string reason, IDictionary<string, object?> record)
    {
        ReportError(reason, record);
        _rejections.Add(new RejectedRecord(stage, reason, record));
    }

    /// <summary>
    /// Minimal service provider returned when no DI container is available.
    /// </summary>
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
