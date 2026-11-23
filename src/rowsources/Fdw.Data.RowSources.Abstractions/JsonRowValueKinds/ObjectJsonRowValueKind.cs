using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Fdw.Collections.Attributes;

namespace Fdw.Data.RowSources.Json.Abstractions;

/// <summary>An object: flattened into the row, or stored as its JSON text.</summary>
[TypeOption(typeof(JsonRowValueKinds), "Object")]
[ExcludeFromCodeCoverage]
public sealed class ObjectJsonRowValueKind : JsonRowValueKindBase
{
    /// <summary>Initializes a new instance of the <see cref="ObjectJsonRowValueKind"/> class.</summary>
    public ObjectJsonRowValueKind()
        : base("Object", JsonValueKind.Object)
    {
    }

    /// <inheritdoc />
    public override void Visit(IJsonRowSink sink, string name, JsonElement element, bool nestedFlattened)
    {
        if (nestedFlattened)
        {
            sink.FlattenObject(element, name);
        }
        else
        {
            sink.AddValue(name, element.GetRawText());
        }
    }
}
