using System.Collections.Generic;
using Fdw.Orchestration.Abstractions;

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
// Why: Previously redeclared ExecutionId (Guid) and Logger independently.
// All universal fields are now inherited from IExecutionContext.
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
    /// Reports a transform error.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <param name="record">The failed record.</param>
    void ReportError(string error, IDictionary<string, object?>? record = null);
}
