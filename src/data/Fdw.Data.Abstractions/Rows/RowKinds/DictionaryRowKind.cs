using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Data.Abstractions.Rows;

/// <summary>A dictionary of column values, what a pipeline load produces.</summary>
[TypeOption(typeof(RowKinds), "Dictionary")]
[ExcludeFromCodeCoverage]
public sealed class DictionaryRowKind : RowKindBase
{
    /// <summary>Initializes a new instance of the <see cref="DictionaryRowKind"/> class.</summary>
    public DictionaryRowKind()
        : base("Dictionary")
    {
    }

    /// <inheritdoc />
    public override bool Holds(object row) => row is IDictionary<string, object?>;

    /// <inheritdoc />
    public override RowReading Read(object row, string column)
    {
        var dictionary = (IDictionary<string, object?>)row;
        return Find(dictionary.Keys, key => dictionary[key], column);
    }

    /// <inheritdoc />
    public override Type? BindingType(RowReading reading, Type columnType) => DictionaryBinding(reading, columnType);
}
