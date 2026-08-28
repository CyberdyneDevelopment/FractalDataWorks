using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Orchestration.Abstractions;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;

namespace Fdw.Calculations;

/// <summary>
/// Execution context for calculations.
/// </summary>
/// <remarks>
/// Extends <see cref="IExecutionContext"/> with calculation-specific capabilities:
/// data gateway access and typed caches for datasets and intermediate results.
/// Universal per-run fields (ExecutionId, StartTime, CancellationToken, Logger,
/// Services, Parameters, SharedState) are inherited from <see cref="IExecutionContext"/>.
/// </remarks>
public interface ICalculationContext : IExecutionContext
{
    /// <summary>
    /// Gets the data gateway for accessing multiple data sources.
    /// Enables calculations to query multiple databases, APIs, files, etc.
    /// </summary>
    IDataGateway DataGateway { get; }

    /// <summary>
    /// Gets data from a source using connection name and container name.
    /// Convenience method for simple query scenarios.
    /// </summary>
    /// <typeparam name="TData">The expected data type.</typeparam>
    /// <param name="connectionName">The name of the connection.</param>
    /// <param name="containerName">The name of the container (table, collection, etc.).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result containing the data or failure information.</returns>
    Task<IGenericResult<TData>> GetData<TData>(
        string connectionName,
        string containerName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets data using a custom data command for complex queries.
    /// Supports filtering, joins, aggregations, etc. via IDataCommand.
    /// Addressing (DataStore, Path, Container) is supplied separately via <paramref name="target"/>.
    /// </summary>
    /// <typeparam name="TData">The expected data type.</typeparam>
    /// <param name="command">The data command to execute (address-free shape: filter/ordering/paging).</param>
    /// <param name="target">The DataStore target that identifies which container to query.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result containing the data or failure information.</returns>
    Task<IGenericResult<TData>> GetData<TData>(
        IDataCommand command,
        DataStoreTarget target,
        CancellationToken cancellationToken = default);

    #region Dataset Convenience Methods

    /// <summary>
    /// Gets a cached dataset from shared state.
    /// </summary>
    /// <typeparam name="T">The dataset type.</typeparam>
    /// <param name="name">The dataset name.</param>
    /// <returns>The dataset, or default if not found.</returns>
    T? GetDataSet<T>(string name);

    /// <summary>
    /// Tries to get a cached dataset from shared state.
    /// </summary>
    /// <typeparam name="T">The dataset type.</typeparam>
    /// <param name="name">The dataset name.</param>
    /// <param name="dataset">The dataset if found.</param>
    /// <returns>True if the dataset exists, false otherwise.</returns>
    bool TryGetDataSet<T>(string name, [NotNullWhen(true)] out T? dataset);

    /// <summary>
    /// Adds or updates a dataset in the shared state cache.
    /// </summary>
    /// <typeparam name="T">The dataset type.</typeparam>
    /// <param name="name">The dataset name.</param>
    /// <param name="dataset">The dataset to cache.</param>
    void SetDataSet<T>(string name, T dataset);

    /// <summary>
    /// Checks if a dataset exists in the shared state cache.
    /// </summary>
    /// <param name="name">The dataset name.</param>
    /// <returns>True if the dataset exists, false otherwise.</returns>
    bool HasDataSet(string name);

    #endregion

    #region Calculation Result Convenience Methods

    /// <summary>
    /// Gets a cached calculation result from shared state.
    /// </summary>
    /// <typeparam name="T">The result type.</typeparam>
    /// <param name="calculationName">The calculation name.</param>
    /// <returns>The result, or default if not found.</returns>
    T? GetCalculationResult<T>(string calculationName);

    /// <summary>
    /// Tries to get a cached calculation result from shared state.
    /// </summary>
    /// <typeparam name="T">The result type.</typeparam>
    /// <param name="calculationName">The calculation name.</param>
    /// <param name="result">The result if found.</param>
    /// <returns>True if the result exists, false otherwise.</returns>
    bool TryGetCalculationResult<T>(string calculationName, [NotNullWhen(true)] out T? result);

    /// <summary>
    /// Stores a calculation result in the shared state cache.
    /// </summary>
    /// <typeparam name="T">The result type.</typeparam>
    /// <param name="calculationName">The calculation name.</param>
    /// <param name="result">The result to cache.</param>
    void SetCalculationResult<T>(string calculationName, T result);

    /// <summary>
    /// Checks if a calculation result exists in the shared state cache.
    /// </summary>
    /// <param name="calculationName">The calculation name.</param>
    /// <returns>True if the result exists, false otherwise.</returns>
    bool HasCalculationResult(string calculationName);

    #endregion
}
