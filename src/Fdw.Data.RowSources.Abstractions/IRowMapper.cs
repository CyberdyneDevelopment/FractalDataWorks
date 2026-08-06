using System.Collections.Generic;
using Fdw.Data.Abstractions;

namespace Fdw.Data.RowSources.Abstractions;

/// <summary>
/// Source-agnostic row mapper that converts <see cref="IRecordCursor"/> data to dictionaries.
/// Works with any record cursor (DataReader, XML, JSON, HTTP streams).
/// </summary>
/// <remarks>
/// Unlike IEtlRowMapper which is coupled to IDataReader, this interface
/// works with the abstracted <see cref="IRecordCursor"/>, enabling unified mapping logic
/// across all data formats.
/// </remarks>
public interface IRowMapper
{
    /// <summary>
    /// Initializes the mapper with a container schema.
    /// Call this once before mapping any rows to pre-compute ordinals and converters.
    /// </summary>
    /// <param name="container">The container with schema metadata.</param>
    void Initialize(IStorageContainer container);

    /// <summary>
    /// Maps the current row from the source to a dictionary.
    /// The source must be positioned on a valid row before calling this method.
    /// </summary>
    /// <param name="source">The record cursor positioned on a row.</param>
    /// <returns>A dictionary containing the row values.</returns>
    IDictionary<string, object?> MapRow(IRecordCursor source);

    /// <summary>
    /// Returns a dictionary to the mapper for potential reuse.
    /// Call this after processing each row to enable object pooling.
    /// </summary>
    /// <param name="row">The dictionary to return.</param>
    void ReturnRow(IDictionary<string, object?> row);

    /// <summary>
    /// Gets the estimated number of allocations per row for this mapper.
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
    /// Call this when switching to a different container.
    /// </summary>
    void Reset();
}
