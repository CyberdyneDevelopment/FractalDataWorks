using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Fdw.Collections.Attributes;

namespace Fdw.Data.Abstractions.Rows;

/// <summary>An entity whose public properties are the columns; a column is found by its property name regardless of case.</summary>
[TypeOption(typeof(RowKinds), "Entity")]
[ExcludeFromCodeCoverage]
public sealed class EntityRowKind : RowKindBase
{
    /// <summary>Initializes a new instance of the <see cref="EntityRowKind"/> class.</summary>
    public EntityRowKind()
        : base("Entity")
    {
    }

    /// <inheritdoc />
    public override bool Holds(object row) => row is not IDictionary<string, object?> and not IReadOnlyDictionary<string, object?>;

    /// <inheritdoc />
    public override RowReading Read(object row, string column)
    {
        var property = row.GetType().GetProperty(column, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        return property is null ? new RowReading(false, null, null) : new RowReading(true, property.GetValue(row), property.PropertyType);
    }

    /// <inheritdoc />
    public override Type? BindingType(RowReading reading, Type columnType) => reading.DeclaredType;
}
