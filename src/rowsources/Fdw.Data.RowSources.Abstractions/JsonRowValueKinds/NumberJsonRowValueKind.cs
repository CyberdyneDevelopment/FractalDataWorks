using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Fdw.Collections.Attributes;

namespace Fdw.Data.RowSources.Json.Abstractions;

/// <summary>A number: a long when it is whole and fits one, else a double, else its JSON text.</summary>
[TypeOption(typeof(JsonRowValueKinds), "Number")]
[ExcludeFromCodeCoverage]
public sealed class NumberJsonRowValueKind : JsonRowValueKindBase
{
    /// <summary>Initializes a new instance of the <see cref="NumberJsonRowValueKind"/> class.</summary>
    public NumberJsonRowValueKind()
        : base("Number", JsonValueKind.Number)
    {
    }

    /// <inheritdoc />
    public override void Visit(IJsonRowSink sink, string name, JsonElement element, bool nestedFlattened)
    {
        if (element.TryGetInt64(out var whole))
        {
            sink.AddValue(name, whole);
        }
        else if (element.TryGetDouble(out var fractional))
        {
            sink.AddValue(name, fractional);
        }
        else
        {
            sink.AddValue(name, element.GetRawText());
        }
    }
}
