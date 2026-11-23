using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Data.RowSources.Json.Abstractions;

/// <summary>The kinds of JSON value a row reader meets, each found by its <see cref="JsonValueKind"/>.</summary>
[TypeCollection(typeof(JsonRowValueKindBase), typeof(IJsonRowValueKind), typeof(JsonRowValueKinds))]
[ExcludeFromCodeCoverage]
public abstract partial class JsonRowValueKinds : TypeCollectionBase<JsonRowValueKindBase, IJsonRowValueKind>
{
    /// <summary>Puts a JSON value into the row by the kind it is.</summary>
    /// <param name="sink">Where the value goes.</param>
    /// <param name="name">The flattened field name.</param>
    /// <param name="element">The value.</param>
    /// <param name="nestedFlattened">Whether a nested object or array is flattened into the row.</param>
    /// <exception cref="InvalidOperationException">No kind reads the value's JSON value kind.</exception>
    public static void Visit(IJsonRowSink sink, string name, JsonElement element, bool nestedFlattened)
    {
        var kind = ByValueKind(element.ValueKind);
        if (ReferenceEquals(kind, NotFound))
        {
            throw new InvalidOperationException($"A JSON value of kind {element.ValueKind} has no row value kind, so field '{name}' cannot be read.");
        }

        kind.Visit(sink, name, element, nestedFlattened);
    }
}
