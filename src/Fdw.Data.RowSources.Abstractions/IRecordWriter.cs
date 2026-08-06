using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Fdw.Data.RowSources.Abstractions;

/// <summary>
/// The parent abstraction over any target that accepts RECORDS (items) and serializes them to a
/// format: the write-side mirror of <see cref="IRecordSource{T}"/>. A JSON/XML writer accepts items;
/// a delimited/fixed-width writer accepts rows — both are records.
/// </summary>
/// <typeparam name="T">
/// The record type. With no compile-time DTO this is <see cref="DataRecord"/> — the configured field set
/// IS the type. When a DTO exists the writer is <c>IRecordWriter&lt;TDto&gt;</c> and each DTO is projected
/// onto the configured fields before serialization.
/// </typeparam>
/// <remarks>
/// This is the parent of the writer hierarchy:
/// <list type="bullet">
/// <item><description>Json/Xml writers accept items and implement <c>IRecordWriter&lt;T&gt;</c> only.</description></item>
/// <item><description>Delimited/FixedWidth writers are row-oriented and implement the child
/// <see cref="IRowWriter"/> (which is itself an <c>IRecordWriter&lt;DataRecord&gt;</c>).</description></item>
/// </list>
/// A writer is built from a container's configured format + options + field schema; the column/field
/// schema is read from those options, never from a compile-time POCO.
/// </remarks>
public interface IRecordWriter<T> : IDisposable, IAsyncDisposable
{
    /// <summary>
    /// Writes a single record to the target.
    /// </summary>
    /// <param name="record">The record to write.</param>
    void Write(T record);

    /// <summary>
    /// Writes all records in the supplied sequence to the target.
    /// </summary>
    /// <param name="records">The records to write.</param>
    void Write(IEnumerable<T> records);

    /// <summary>
    /// Asynchronously writes all records in the supplied sequence to the target.
    /// </summary>
    /// <param name="records">The records to write.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when all records have been written.</returns>
    ValueTask Write(IAsyncEnumerable<T> records, CancellationToken cancellationToken = default);

    /// <summary>
    /// Flushes any buffered output to the underlying target.
    /// </summary>
    void Flush();
}
