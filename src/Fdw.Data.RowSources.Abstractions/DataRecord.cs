using System;
using System.Collections.Generic;
using Fdw.Data.Abstractions;

namespace Fdw.Data.RowSources.Abstractions;

/// <summary>
/// The default record type for the no-DTO case: a record whose "type" IS the configured field set.
/// It pairs a contiguous array of field VALUES with the shared <see cref="RecordSchema"/> flyweight,
/// and exposes the values as a <see cref="ReadOnlySpan{T}"/> window over that array — the schema is
/// shared once across every record, never re-described per record.
/// </summary>
/// <remarks>
/// Why a <see langword="readonly struct"/> (and not a <c>ref struct</c>): a record must be usable as the
/// generic argument <c>T</c> of <see cref="IRecordSource{T}"/> (the no-DTO source is
/// <c>IRecordSource&lt;DataRecord&gt;</c>) and must flow through <c>IGenericResult&lt;DataRecord&gt;</c>
/// and <c>IAsyncEnumerable&lt;...&gt;</c>. A <c>ref struct</c> cannot be a type argument, so a true
/// <c>ReadOnlySpan&lt;object?&gt;</c>-backed record cannot itself be the record type. The chosen design is a
/// <see langword="readonly struct"/> that <em>wraps</em> the value array (a flyweight schema + one
/// <c>object?[]</c> per materialized record) and EXPOSES a <see cref="Values"/> span so callers run
/// zero-copy windowed reads over the buffer. This is the "array wrapper + Span accessor" choice the
/// design called out as the fallback when a ref struct fights the generic.
/// <para>
/// The fields ARE the type: there is no compile-time class. <see cref="this[int]"/> / <see cref="this[string]"/>
/// read cells through the shared schema; <see cref="Schema"/> is the flyweight describing every cell.
/// </para>
/// </remarks>
public readonly struct DataRecord : IEquatable<DataRecord>
{
    private readonly object?[] _values;
    private readonly RecordSchema _schema;

    /// <summary>
    /// Initializes a new instance of the <see cref="DataRecord"/> struct over the supplied values and
    /// shared schema. The caller owns the lifetime of <paramref name="values"/>; the record does not copy it.
    /// </summary>
    /// <param name="schema">The shared flyweight schema describing each value position.</param>
    /// <param name="values">
    /// The field values, positionally aligned to <paramref name="schema"/>. Length must equal
    /// <see cref="RecordSchema.FieldCount"/>.
    /// </param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="schema"/> or <paramref name="values"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the value count does not match the schema field count.</exception>
    public DataRecord(RecordSchema schema, object?[] values)
    {
        _schema = schema ?? throw new ArgumentNullException(nameof(schema));
        _values = values ?? throw new ArgumentNullException(nameof(values));
        if (values.Length != schema.FieldCount)
        {
            throw new ArgumentException(
                $"Value count ({values.Length}) does not match schema field count ({schema.FieldCount}).",
                nameof(values));
        }
    }

    /// <summary>
    /// Gets the shared flyweight schema describing every value position in this record.
    /// </summary>
    public RecordSchema Schema => _schema;

    /// <summary>
    /// Gets the number of fields in this record (equal to <see cref="RecordSchema.FieldCount"/>).
    /// </summary>
    public int FieldCount => _schema?.FieldCount ?? 0;

    /// <summary>
    /// Gets the field values as a read-only span window over the backing array — zero-copy access for
    /// bulk/windowed reads. The span is interpreted positionally through <see cref="Schema"/>.
    /// </summary>
    public ReadOnlySpan<object?> Values => _values;

    /// <summary>
    /// Gets the value at the specified ordinal.
    /// </summary>
    /// <param name="ordinal">The zero-based field index.</param>
    /// <returns>The value, or null when the cell is null.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="ordinal"/> is out of range.</exception>
    public object? this[int ordinal]
    {
        get
        {
            if (ordinal < 0 || ordinal >= _values.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(ordinal));
            }

            return _values[ordinal];
        }
    }

    /// <summary>
    /// Gets the value of the field with the given name.
    /// </summary>
    /// <param name="fieldName">The field name (case-insensitive), resolved through the shared schema.</param>
    /// <returns>The value, or null when the cell is null or the field is absent.</returns>
    public object? this[string fieldName]
    {
        get
        {
            var ordinal = _schema.GetFieldOrdinal(fieldName);
            return ordinal < 0 ? null : _values[ordinal];
        }
    }

    /// <inheritdoc />
    public bool Equals(DataRecord other)
        => ReferenceEquals(_schema, other._schema) && ReferenceEquals(_values, other._values);

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is DataRecord other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        unchecked
        {
            var hash = _schema is null ? 0 : _schema.GetHashCode();
            return (hash * 397) ^ (_values is null ? 0 : _values.GetHashCode());
        }
    }

    /// <summary>
    /// Determines whether two records view the same buffer through the same schema.
    /// </summary>
    public static bool operator ==(DataRecord left, DataRecord right) => left.Equals(right);

    /// <summary>
    /// Determines whether two records do not view the same buffer through the same schema.
    /// </summary>
    public static bool operator !=(DataRecord left, DataRecord right) => !left.Equals(right);

    /// <summary>
    /// Projects this record into a flat name→value map. Allocates a dictionary — use only when a
    /// materialized map is genuinely needed; prefer <see cref="Values"/> / ordinal access on the hot path.
    /// </summary>
    /// <returns>A case-insensitive map of field name to value.</returns>
    public IReadOnlyDictionary<string, object?> ToDictionary()
    {
        var map = new Dictionary<string, object?>(_values.Length, StringComparer.OrdinalIgnoreCase);
        var fields = _schema.Fields;
        for (var i = 0; i < _values.Length; i++)
        {
            map[fields[i].Name] = _values[i];
        }

        return map;
    }
}
