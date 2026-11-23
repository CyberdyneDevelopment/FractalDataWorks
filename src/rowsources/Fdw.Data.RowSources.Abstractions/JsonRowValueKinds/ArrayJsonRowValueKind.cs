using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Fdw.Collections.Attributes;

namespace Fdw.Data.RowSources.Json.Abstractions;

/// <summary>An array: flattened into the row by index, or stored as its JSON text.</summary>
[TypeOption(typeof(JsonRowValueKinds), "Array")]
[ExcludeFromCodeCoverage]
public sealed class ArrayJsonRowValueKind : JsonRowValueKindBase
{
    /// <summary>Initializes a new instance of the <see cref="ArrayJsonRowValueKind"/> class.</summary>
    public ArrayJsonRowValueKind()
        : base("Array", JsonValueKind.Array)
    {
    }

    /// <inheritdoc />
    public override void Visit(IJsonRowSink sink, string name, JsonElement element, bool nestedFlattened)
    {
        if (nestedFlattened)
        {
            sink.FlattenArray(element, name);
        }
        else
        {
            sink.AddValue(name, element.GetRawText());
        }
    }
}
