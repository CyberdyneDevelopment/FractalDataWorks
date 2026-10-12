using System;
using System.Collections.Generic;
using System.Threading;
using Fdw.Results;

namespace Fdw.Data.RowSources.Abstractions;

/// <summary>
/// The parent abstraction over any source that yields RECORDS (items): a JSON/XML document yields
/// items, a delimited/fixed-width/tabular source yields rows — both are records. A record source is
/// built from a container's configured format + options + field schema and enumerated for the records
/// it contains.
/// </summary>
/// <typeparam name="T">
/// The record type. With no compile-time DTO this is <see cref="DataRecord"/> — the configured field
/// set IS the type (a value array over the shared <see cref="Schema"/> flyweight). When a DTO exists the
/// source is <c>IRecordSource&lt;TDto&gt;</c> and each record is projected into <typeparamref name="T"/>.
/// </typeparam>
/// <remarks>
/// This is the parent of the format hierarchy:
/// <list type="bullet">
/// <item><description>Json/Xml sources yield items and implement <c>IRecordSource&lt;T&gt;</c> only.</description></item>
/// <item><description>Delimited/FixedWidth/Tabular/DataReader sources are row-oriented and implement the
/// child <see cref="IRowSource"/> (which is itself an <c>IRecordSource&lt;DataRecord&gt;</c>).</description></item>
/// </list>
/// Records are produced lazily. Each enumerated element is an <see cref="IGenericResult{T}"/> so a
/// per-record parse/convert failure surfaces as a failed result WITHOUT stopping enumeration or
/// throwing (callers MUST check <c>IsSuccess</c> before <c>.Value</c>). The <see cref="Schema"/> is the
/// shared flyweight — described once, never per record.
/// </remarks>
public interface IRecordSource<T> : IDisposable, IAsyncDisposable
{
    /// <summary>
    /// Gets the shared flyweight schema for every record this source yields. Described once; never
    /// re-described per record.
    /// </summary>
    RecordSchema Schema { get; }

    /// <summary>
    /// Enumerates the records synchronously, yielding each as an <see cref="IGenericResult{T}"/>.
    /// </summary>
    /// <returns>
    /// A lazy sequence of per-record results. A successful element carries the record; a failed element
    /// carries the parse/convert error and enumeration continues.
    /// </returns>
    /// <remarks>
    /// Sources that cannot read synchronously (forward-only network streams) should expose only the
    /// async <see cref="Read(CancellationToken)"/> overload and fail loud from this method rather than
    /// block on async work.
    /// </remarks>
    IEnumerable<IGenericResult<T>> Read();

    /// <summary>
    /// Enumerates the records asynchronously, yielding each as an <see cref="IGenericResult{T}"/>.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for aborting enumeration.</param>
    /// <returns>
    /// A lazy async sequence of per-record results. A successful element carries the record; a failed
    /// element carries the parse/convert error and enumeration continues.
    /// </returns>
    /// <remarks>
    /// Why: distinguished from the synchronous <see cref="Read()"/> by OVERLOAD (the
    /// <see cref="CancellationToken"/> parameter), never by an <c>Async</c> name suffix (FDW001).
    /// </remarks>
    IAsyncEnumerable<IGenericResult<T>> Read(CancellationToken cancellationToken = default);
}
