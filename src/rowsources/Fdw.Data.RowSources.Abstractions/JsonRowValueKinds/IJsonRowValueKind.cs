using System.Text.Json;
using Fdw.Collections;

namespace Fdw.Data.RowSources.Json.Abstractions;

/// <summary>What a row reader does with a JSON value of one kind.</summary>
public interface IJsonRowValueKind : ITypeOption<int, JsonRowValueKindBase>
{
    /// <summary>Gets the JSON value kind this reads.</summary>
    JsonValueKind ValueKind { get; }

    /// <summary>Puts a value of this kind into the row.</summary>
    /// <param name="sink">Where the value goes.</param>
    /// <param name="name">The flattened field name.</param>
    /// <param name="element">The value.</param>
    /// <param name="nestedFlattened">Whether a nested object or array is flattened into the row; when false it is stored as its JSON text.</param>
    void Visit(IJsonRowSink sink, string name, JsonElement element, bool nestedFlattened);
}
