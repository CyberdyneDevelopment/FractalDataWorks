using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Fdw.Data.RowSources.Abstractions;

/// <summary>
/// The row-oriented specialization of <see cref="IRecordWriter{T}"/> for a target that can accept a FULL,
/// schema-agnostic flat name→value row without losing any of its columns — either because the target is
/// genuinely tabular (Delimited, FixedWidth) or because its document shape has no fixed-column constraint
/// at all (Json). The write-side mirror of <see cref="IRowSource"/>: it accepts row records — both the
/// strongly-typed <see cref="DataRecord"/> (via the inherited <see cref="IRecordWriter{T}"/> surface) and
/// the schema-agnostic flat name→value-map shape used on the existing write path — and serializes them to
/// a target <see cref="System.IO.TextWriter"/> in a specific format.
/// </summary>
/// <remarks>
/// As an <c>IRecordWriter&lt;DataRecord&gt;</c> a row writer is a record writer whose record type is the
/// fixed positional row. The dictionary overloads remain so the existing dynamic-row write path keeps
/// working: a writer produced for a given format reads its column/field schema from the format options
/// supplied at construction, never from a compile-time POCO.
/// <para>
/// Xml is NOT a row writer — its output shape is the document's, not a fixed column layout — so it
/// implements <see cref="IRecordWriter{T}"/> only; writing an XML row therefore projects the row onto the
/// container's declared field schema via <see cref="DataRecord"/>, dropping any undeclared column. Json,
/// by contrast, DOES implement <see cref="IRowWriter"/> (see <see cref="Fdw.Data.RowSources.Json.Abstractions.JsonStreamRowWriter"/>):
/// a JSON object has no fixed-column constraint, so its writer accepts and preserves the full row
/// dictionary — every key, not just the declared ones — symmetric with its read side
/// (<c>JsonStreamRowSource</c>), which already decodes every JSON property dynamically.
/// </para>
/// </remarks>
public interface IRowWriter : IRecordWriter<DataRecord>
{
    /// <summary>
    /// Writes a single row to the target.
    /// </summary>
    /// <param name="row">The row as a flat name→value map.</param>
    void Write(IReadOnlyDictionary<string, object?> row);

    /// <summary>
    /// Writes all rows in the supplied sequence to the target.
    /// </summary>
    /// <param name="rows">The rows to write.</param>
    void WriteAll(IEnumerable<IReadOnlyDictionary<string, object?>> rows);

    /// <summary>
    /// Asynchronously writes all rows in the supplied sequence to the target.
    /// </summary>
    /// <param name="rows">The rows to write.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when all rows have been written.</returns>
    ValueTask WriteAll(IEnumerable<IReadOnlyDictionary<string, object?>> rows, CancellationToken cancellationToken);

    // Flush(), Dispose(), and DisposeAsync() are inherited from IRecordWriter<DataRecord>.
}
