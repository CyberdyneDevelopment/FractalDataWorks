using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Data.DataContainers.Abstractions.ContainerWriteModeOptions;

namespace Fdw.Data.DataContainers.Abstractions;

/// <summary>
/// Interface for writing data to containers in a streaming fashion.
/// Provides both synchronous and asynchronous writing of records.
/// </summary>
/// <remarks>
/// IDataWriter abstracts the writing process across different container formats.
/// It provides a consistent interface whether writing to CSV files, SQL tables,
/// JSON documents, or any other supported format. The writer handles format-specific
/// serialization and validation while presenting a uniform API.
/// </remarks>
public interface IDataWriter : IDisposable
{
    /// <summary>
    /// Gets the schema that data written to this writer must conform to.
    /// </summary>
    /// <value>The schema describing required field names, types, and constraints.</value>
    IDataSchema Schema { get; }

    /// <summary>
    /// Gets the current position within the data stream.
    /// </summary>
    /// <value>The current record number being written, starting from 0.</value>
    long CurrentPosition { get; }

    /// <summary>
    /// Gets metadata about the data destination being written to.
    /// </summary>
    /// <value>Additional information about the destination.</value>
    IReadOnlyDictionary<string, object> DestinationMetadata { get; }

    /// <summary>
    /// Gets the write mode being used (overwrite, append, etc.).
    /// </summary>
    /// <value>The write mode that was specified when creating this writer.</value>
    IContainerWriteMode WriteMode { get; }

    /// <summary>
    /// Gets a value indicating whether the writer supports batch operations.
    /// </summary>
    /// <value><c>true</c> if batch writing is supported; otherwise, <c>false</c>.</value>
    bool SupportsBatch { get; }

    /// <summary>
    /// Gets a value indicating whether the writer supports transactions.
    /// </summary>
    /// <value><c>true</c> if transactional writing is supported; otherwise, <c>false</c>.</value>
    bool SupportsTransactions { get; }

    /// <summary>
    /// Writes a single record from a dictionary of field names to values.
    /// </summary>
    /// <param name="record">The record to write as key-value pairs.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A result indicating whether the write operation succeeded.</returns>
    /// <remarks>
    /// This method provides dynamic writing of record data without requiring
    /// compile-time knowledge of the record structure. Field values should match
    /// the types specified in the writer's schema.
    /// </remarks>
    Task<IGenericResult> WriteRecord(
        IReadOnlyDictionary<string, object> record,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Writes a single record from a strongly-typed object.
    /// </summary>
    /// <typeparam name="T">The type of the record object.</typeparam>
    /// <param name="record">The record object to write.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A result indicating whether the write operation succeeded.</returns>
    /// <remarks>
    /// This method provides strongly-typed writing with automatic serialization
    /// and validation. Properties are mapped to schema fields based on name and type.
    /// </remarks>
    Task<IGenericResult> WriteRecord<T>(
        T record,
        CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Writes multiple records from dictionaries.
    /// </summary>
    /// <param name="records">The records to write as key-value pairs.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A result indicating how many records were successfully written.</returns>
    /// <remarks>
    /// This method is optimized for bulk writing operations. It may be more
    /// efficient than calling WriteRecord repeatedly for large datasets.
    /// If any record fails validation, the operation may stop early.
    /// </remarks>
    Task<IGenericResult<long>> WriteRecords(
        IEnumerable<IReadOnlyDictionary<string, object>> records,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Writes multiple records from strongly-typed objects.
    /// </summary>
    /// <typeparam name="T">The type of the record objects.</typeparam>
    /// <param name="records">The record objects to write.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A result indicating how many records were successfully written.</returns>
    /// <remarks>
    /// This method is optimized for bulk writing operations with type mapping.
    /// It provides better performance than individual record writing for large datasets.
    /// If any record fails validation, the operation may stop early.
    /// </remarks>
    Task<IGenericResult<long>> WriteRecords<T>(
        IEnumerable<T> records,
        CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Flushes any buffered data to the underlying storage.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A result indicating whether the flush operation succeeded.</returns>
    /// <remarks>
    /// This method ensures that all buffered data is written to the destination.
    /// Some container types may buffer writes for performance, so explicit flushing
    /// may be necessary to ensure data persistence.
    /// </remarks>
    Task<IGenericResult> Flush(CancellationToken cancellationToken = default);

    /// <summary>
    /// Finalizes the writing operation and closes the writer.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A result containing the final write statistics.</returns>
    /// <remarks>
    /// This method performs final cleanup and validation of the written data.
    /// It should be called when all writing is complete. After calling this method,
    /// the writer should not be used for further write operations.
    /// </remarks>
    Task<IGenericResult<IWriterStatistics>> Finalize(CancellationToken cancellationToken = default);

    /// <summary>
    /// Begins a transaction for atomic write operations.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A result containing the transaction instance, if transactions are supported.</returns>
    /// <remarks>
    /// This method is only supported if SupportsTransactions returns true.
    /// Transactions ensure that either all writes succeed or none do.
    /// </remarks>
    Task<IGenericResult<IWriteTransaction>> BeginTransaction(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets statistics about the writing operation so far.
    /// </summary>
    /// <returns>Statistics about records written, time elapsed, etc.</returns>
    /// <remarks>
    /// This method provides performance and progress information that can be
    /// useful for monitoring long-running write operations.
    /// </remarks>
    IWriterStatistics GetStatistics();
}

