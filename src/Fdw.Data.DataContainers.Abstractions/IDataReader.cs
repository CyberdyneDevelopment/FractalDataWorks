using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.Abstractions;
using Fdw.Results;

namespace Fdw.Data.DataContainers.Abstractions;

/// <summary>
/// Interface for reading data from containers in a streaming fashion.
/// Provides both synchronous and asynchronous enumeration of records.
/// </summary>
/// <remarks>
/// IDataReader abstracts the reading process across different container formats.
/// It provides a consistent interface whether reading from CSV files, SQL tables,
/// JSON documents, or any other supported format. The reader handles format-specific
/// parsing and type conversion while presenting a uniform API.
/// </remarks>
public interface IDataReader : IDisposable
{
    /// <summary>
    /// Gets the schema of data being read from this reader.
    /// </summary>
    /// <value>The schema describing field names, types, and constraints.</value>
    IDataSchema Schema { get; }

    /// <summary>
    /// Gets the current position within the data stream.
    /// </summary>
    /// <value>The current record number, or -1 if before the first record.</value>
    long CurrentPosition { get; }

    /// <summary>
    /// Gets metadata about the data source being read.
    /// </summary>
    /// <value>Additional information about the source data.</value>
    IReadOnlyDictionary<string, object> SourceMetadata { get; }

    /// <summary>
    /// Gets a value indicating whether the reader has more records to read.
    /// </summary>
    /// <value><c>true</c> if more records are available; otherwise, <c>false</c>.</value>
    bool HasMoreRecords { get; }

    /// <summary>
    /// Gets a value indicating whether the reader supports seeking to specific positions.
    /// </summary>
    /// <value><c>true</c> if seeking is supported; otherwise, <c>false</c>.</value>
    bool SupportsSeek { get; }

    /// <summary>
    /// Reads the next record as a dictionary of field names to values.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>
    /// The next record as key-value pairs, or null if no more records are available.
    /// </returns>
    /// <remarks>
    /// This method provides dynamic access to record data without requiring
    /// compile-time knowledge of the record structure. Field values are returned
    /// as objects and may need casting to their actual types.
    /// </remarks>
    Task<IGenericResult<IReadOnlyDictionary<string, object>?>> ReadRecord(CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads the next record and maps it to a strongly-typed object.
    /// </summary>
    /// <typeparam name="T">The type to map the record to.</typeparam>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>
    /// The next record mapped to the specified type, or null if no more records are available.
    /// </returns>
    /// <remarks>
    /// This method provides strongly-typed access to record data with automatic
    /// type conversion and mapping. The mapping is based on property names and
    /// the container's schema information.
    /// </remarks>
    Task<IGenericResult<T?>> ReadRecord<T>(CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Reads multiple records as dictionaries.
    /// </summary>
    /// <param name="maxRecords">The maximum number of records to read.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A collection of records as key-value pairs.</returns>
    /// <remarks>
    /// This method is optimized for bulk reading operations. It may be more
    /// efficient than calling ReadRecord repeatedly for large datasets.
    /// </remarks>
    Task<IGenericResult<IEnumerable<IReadOnlyDictionary<string, object>>>> ReadRecords(
        int maxRecords,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads multiple records and maps them to strongly-typed objects.
    /// </summary>
    /// <typeparam name="T">The type to map records to.</typeparam>
    /// <param name="maxRecords">The maximum number of records to read.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A collection of records mapped to the specified type.</returns>
    /// <remarks>
    /// This method is optimized for bulk reading operations with type mapping.
    /// It provides better performance than individual record reading for large datasets.
    /// </remarks>
    Task<IGenericResult<IEnumerable<T>>> ReadRecords<T>(
        int maxRecords,
        CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Reads all remaining records as dictionaries.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>All remaining records as key-value pairs.</returns>
    /// <remarks>
    /// Use this method with caution on large datasets as it loads all data into memory.
    /// Consider using ReadRecords with batching for better memory management.
    /// </remarks>
    Task<IGenericResult<IEnumerable<IReadOnlyDictionary<string, object>>>> ReadAllRecords(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads all remaining records and maps them to strongly-typed objects.
    /// </summary>
    /// <typeparam name="T">The type to map records to.</typeparam>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>All remaining records mapped to the specified type.</returns>
    /// <remarks>
    /// Use this method with caution on large datasets as it loads all data into memory.
    /// Consider using ReadRecords with batching for better memory management.
    /// </remarks>
    Task<IGenericResult<IEnumerable<T>>> ReadAllRecords<T>(
        CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Seeks to a specific position in the data stream.
    /// </summary>
    /// <param name="position">The position to seek to (0-based record index).</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A result indicating whether the seek operation succeeded.</returns>
    /// <remarks>
    /// This method is only supported if SupportsSeek returns true. The position
    /// is 0-based, where position 0 represents the first record.
    /// </remarks>
    Task<IGenericResult> Seek(long position, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resets the reader to the beginning of the data stream.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A result indicating whether the reset operation succeeded.</returns>
    /// <remarks>
    /// This method is equivalent to Seek(0) but may be more efficient for
    /// some container types. It's only supported if SupportsSeek returns true.
    /// </remarks>
    Task<IGenericResult> Reset(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets statistics about the reading operation so far.
    /// </summary>
    /// <returns>Statistics about records read, time elapsed, etc.</returns>
    /// <remarks>
    /// This method provides performance and progress information that can be
    /// useful for monitoring long-running read operations.
    /// </remarks>
    IReaderStatistics GetStatistics();
}

