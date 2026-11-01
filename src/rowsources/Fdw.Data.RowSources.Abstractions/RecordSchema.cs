using System;
using System.Collections.Generic;
using Fdw.Data.Abstractions;

namespace Fdw.Data.RowSources.Abstractions;

/// <summary>
/// The shared, immutable field schema for every record produced by a single <see cref="IRecordSource{T}"/>.
/// This is the flyweight: the container's <see cref="IDataField"/> children are described ONCE here and
/// shared across all records of the source, so a record carries only its value array, never a per-record
/// copy of the field descriptions.
/// </summary>
/// <remarks>
/// Why a class (not a struct): a single instance is shared by reference across millions of
/// <see cref="DataRecord"/> values. The fields are the intrinsic state; each record's values are the
/// extrinsic state. Name→ordinal lookup is pre-computed once at construction so per-record access by
/// name is O(1) without re-scanning the field list.
/// </remarks>
public sealed class RecordSchema
{
    private readonly IReadOnlyList<IDataField> _fields;
    private readonly Dictionary<string, int> _ordinalsByName;

    /// <summary>
    /// Initializes a new instance of the <see cref="RecordSchema"/> class from the container's field children.
    /// </summary>
    /// <param name="fields">
    /// The ordered <see cref="IDataField"/> children of the container. The field at index <c>i</c> describes
    /// the value at index <c>i</c> of every <see cref="DataRecord"/> produced against this schema.
    /// </param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="fields"/> is null.</exception>
    public RecordSchema(IReadOnlyList<IDataField> fields)
    {
        _fields = fields ?? throw new ArgumentNullException(nameof(fields));
        _ordinalsByName = new Dictionary<string, int>(_fields.Count, StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < _fields.Count; i++)
        {
            _ordinalsByName[_fields[i].Name] = i;
        }
    }

    /// <summary>
    /// Gets the shared, ordered field definitions. The index of a field is the ordinal of its value
    /// in every <see cref="DataRecord"/> produced against this schema.
    /// </summary>
    public IReadOnlyList<IDataField> Fields => _fields;

    /// <summary>
    /// Gets the number of fields (the required length of each record's value array).
    /// </summary>
    public int FieldCount => _fields.Count;

    /// <summary>
    /// Gets the field name at the specified ordinal.
    /// </summary>
    /// <param name="ordinal">The zero-based field index.</param>
    /// <returns>The field name.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="ordinal"/> is out of range.</exception>
    public string GetFieldName(int ordinal)
    {
        if (ordinal < 0 || ordinal >= _fields.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(ordinal));
        }

        return _fields[ordinal].Name;
    }

    /// <summary>
    /// Gets the zero-based ordinal of the field with the given name.
    /// </summary>
    /// <param name="fieldName">The field name (case-insensitive).</param>
    /// <returns>The zero-based ordinal, or -1 if no field has that name.</returns>
    public int GetFieldOrdinal(string fieldName)
        => !string.IsNullOrEmpty(fieldName) && _ordinalsByName.TryGetValue(fieldName, out var ordinal)
            ? ordinal
            : -1;
}
