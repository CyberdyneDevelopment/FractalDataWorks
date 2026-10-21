using System;
using System.Collections.Generic;
using Fdw.Orchestration.Abstractions;
using Fdw.Services.Calculations.Abstractions.Runtime;

namespace Fdw.Services.Etl.Abstractions;

/// <summary>
/// Context interface for transform execution within an ETL pipeline.
/// </summary>
/// <remarks>
/// Extends <see cref="IExecutionContext"/> with ETL-specific capabilities:
/// pipeline variables, calculation engine access, connection provider access,
/// and error reporting. Universal per-run fields (ExecutionId, StartTime,
/// CancellationToken, Logger, Services, Parameters, SharedState) are inherited
/// from <see cref="IExecutionContext"/>.
/// </remarks>
public interface ITransformContext : IExecutionContext
{
    /// <summary>
    /// Gets pipeline variables that can be used in transforms.
    /// </summary>
    IReadOnlyDictionary<string, object?> Variables { get; }

    /// <summary>
    /// Gets the calculation engine for calculated transforms.
    /// </summary>
    object? CalculationEngine { get; }

    /// <summary>
    /// Gets the connection provider for lookup transforms.
    /// </summary>
    object? ConnectionProvider { get; }

    /// <summary>
    /// Gets the data gateway for executing data commands in lookups.
    /// </summary>
    /// <remarks>
    /// Object-typed to avoid circular references at the abstractions layer.
    /// Cast to IDataGateway at runtime.
    /// </remarks>
    object? DataGateway { get; }

    /// <summary>
    /// Gets the calculations the run compiled for its Compute stages, by the id of the stage. A Compute stage has an entry; a
    /// stage that is not a Compute stage has none. The run resolves and compiles them once, when it starts, so a stage never
    /// reaches the Transformation domain per record.
    /// </summary>
    IReadOnlyDictionary<Guid, ICompiledCalculation> BuiltCalculations { get; }

    /// <summary>
    /// Gets the Built transformations that read and write tables, compiled when the run started, by the id of the Compute stage that
    /// applies each (FDW-1231).
    /// </summary>
    IReadOnlyDictionary<Guid, ICompiledCalculationSet> BuiltSets { get; }

    /// <summary>
    /// Reports a transform error.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <param name="record">The failed record.</param>
    void ReportError(string error, IDictionary<string, object?>? record = null);

    /// <summary>
    /// Rejects a record: it leaves the stream, is reported as an error like any other, and is kept with the stage that
    /// rejected it and why, so the pipeline's reject steps can write it (FDW-1142).
    /// </summary>
    /// <param name="stage">The name of the stage that rejected the record.</param>
    /// <param name="reason">Why the stage rejected it.</param>
    /// <param name="record">The rejected record.</param>
    void Reject(string stage, string reason, IDictionary<string, object?> record);
}
