using System;
using Fdw.Data.Abstractions;

namespace Fdw.Data.RowSources.Abstractions;

/// <summary>
/// The positioned-reader primitive over a single source: a cursor that exposes the field values of
/// the row/item it is currently positioned on, by ordinal, without coupling to <c>IDataReader</c>.
/// </summary>
/// <remarks>
/// This is the low-level ordinal-access cursor that the format readers
/// (<see cref="IRowSourceReader"/>/<see cref="IAsyncRowSourceReader"/>) implement. It is NOT the
/// record-source abstraction — a <see cref="IRecordSource{T}"/> is the thing you enumerate records
/// from; an <see cref="IRecordCursor"/> is the thing a reader uses to expose the current record's
/// values. The two are layered: a record source's enumerator advances an underlying cursor and
/// projects each position into a record (a <see cref="DataRecord"/> or a DTO).
/// <para>
/// Why renamed from <c>IRowSource</c>: the name <c>IRowSource</c> now belongs to the row-oriented
/// child of <see cref="IRecordSource{T}"/> (Delimited/FixedWidth/Tabular). This cursor primitive was
/// never a "source of records"; it is a positioned value-accessor, so it is named <c>IRecordCursor</c>.
/// </para>
/// Unlike IDataReader, this interface:
/// - Does not include schema discovery (use IStorageContainer instead)
/// - Does not include connection/transaction management
/// - Supports integrated type conversion via IDataTypeConverter
/// - Works with XML, JSON, HTTP streams, and traditional ADO.NET sources
/// </remarks>
public interface IRecordCursor : IDisposable
{
    /// <summary>
    /// Gets whether the source is currently positioned on a valid row.
    /// </summary>
    bool HasCurrentRow { get; }

    /// <summary>
    /// Gets the number of fields in each row.
    /// </summary>
    int FieldCount { get; }

    /// <summary>
    /// Gets the field name at the specified ordinal position.
    /// </summary>
    /// <param name="ordinal">The zero-based field index.</param>
    /// <returns>The field name.</returns>
    /// <exception cref="IndexOutOfRangeException">Thrown when ordinal is out of range.</exception>
    string GetFieldName(int ordinal);

    /// <summary>
    /// Gets the ordinal position of a field by name.
    /// </summary>
    /// <param name="fieldName">The field name (case-insensitive).</param>
    /// <returns>The zero-based field index, or -1 if not found.</returns>
    int GetFieldOrdinal(string fieldName);

    /// <summary>
    /// Gets whether the value at the specified ordinal is null.
    /// </summary>
    /// <param name="ordinal">The zero-based field index.</param>
    /// <returns>True if the value is null; otherwise false.</returns>
    bool IsNull(int ordinal);

    /// <summary>
    /// Gets the raw value at the specified ordinal without type conversion.
    /// </summary>
    /// <param name="ordinal">The zero-based field index.</param>
    /// <returns>The raw value, or null if the field is null.</returns>
    object? GetValue(int ordinal);

    /// <summary>
    /// Gets the value at the specified ordinal with type conversion applied.
    /// </summary>
    /// <param name="ordinal">The zero-based field index.</param>
    /// <param name="converter">The data type converter to use.</param>
    /// <returns>The converted value, or null if the field is null.</returns>
    object? GetConvertedValue(int ordinal, IDataTypeConverter converter);

    /// <summary>
    /// Gets the estimated number of allocations per row for performance monitoring.
    /// </summary>
    /// <remarks>
    /// 0 = Zero-allocation after warmup (e.g., pooled buffer reuse)
    /// 1 = One allocation per row (typical)
    /// Higher values indicate more complex sources with nested allocations
    /// </remarks>
    int EstimatedAllocationsPerRow { get; }
}
