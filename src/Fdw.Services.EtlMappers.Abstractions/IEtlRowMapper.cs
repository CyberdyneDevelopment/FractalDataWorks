using System.Collections.Generic;
using System.Data;
using Fdw.Data.Abstractions;

namespace Fdw.Services.EtlMappers.Abstractions;

/// <summary>
/// Interface for ETL row mappers that convert data reader rows to dictionaries.
/// Implementations can optimize for different scenarios (pooled dictionaries, compiled expressions, etc.).
/// </summary>
/// <remarks>
/// ETL mappers are lightweight utilities for data transformation, not full FDW services.
/// They don't participate in the command execution pattern.
/// </remarks>
public interface IEtlRowMapper
{
    /// <summary>
    /// Initializes the mapper with a data reader and container schema.
    /// Call this once before reading any rows to pre-compute ordinals and converters.
    /// </summary>
    /// <param name="reader">The data reader to read from.</param>
    /// <param name="container">The container with schema metadata.</param>
    void Initialize(IDataReader reader, IStorageContainer container);

    /// <summary>
    /// Maps the current row from the data reader to a dictionary.
    /// The reader must be positioned on a valid row before calling this method.
    /// </summary>
    /// <param name="reader">The data reader positioned on a row.</param>
    /// <returns>A dictionary containing the row values.</returns>
    IDictionary<string, object?> MapRow(IDataReader reader);

    /// <summary>
    /// Returns a dictionary to the mapper for potential reuse.
    /// Call this after processing each row to enable object pooling.
    /// </summary>
    /// <param name="row">The dictionary to return.</param>
    void ReturnRow(IDictionary<string, object?> row);

    /// <summary>
    /// Gets the estimated number of allocations per row for this mapper.
    /// Used for performance monitoring and mapper selection.
    /// </summary>
    /// <remarks>
    /// 0 = Pooled/zero-allocation after warmup
    /// 1 = One allocation per row (typical dictionary mapper)
    /// Higher values indicate more allocations
    /// </remarks>
    int EstimatedAllocationsPerRow { get; }

    /// <summary>
    /// Gets whether this mapper has been initialized.
    /// </summary>
    bool IsInitialized { get; }

    /// <summary>
    /// Resets the mapper to its uninitialized state.
    /// Call this when switching to a different reader or container.
    /// </summary>
    void Reset();
}
