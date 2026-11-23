using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Data.Abstractions.Rows;

/// <summary>A read-only dictionary of column values.</summary>
[TypeOption(typeof(RowKinds), "ReadOnlyDictionary")]
[ExcludeFromCodeCoverage]
public sealed class ReadOnlyDictionaryRowKind : RowKindBase
{
    /// <summary>Initializes a new instance of the <see cref="ReadOnlyDictionaryRowKind"/> class.</summary>
    public ReadOnlyDictionaryRowKind()
        : base("ReadOnlyDictionary")
    {
    }

    /// <inheritdoc />
    public override bool Holds(object row) => row is IReadOnlyDictionary<string, object?> and not IDictionary<string, object?>;

    /// <inheritdoc />
    public override RowReading Read(object row, string column)
    {
        var dictionary = (IReadOnlyDictionary<string, object?>)row;
        return Find(dictionary.Keys, key => dictionary[key], column);
    }

    /// <inheritdoc />
    public override Type? BindingType(RowReading reading, Type columnType) => DictionaryBinding(reading, columnType);
}
