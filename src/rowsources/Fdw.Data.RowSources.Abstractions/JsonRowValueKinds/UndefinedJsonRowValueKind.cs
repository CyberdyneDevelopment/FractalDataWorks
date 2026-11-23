using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Fdw.Collections.Attributes;

namespace Fdw.Data.RowSources.Json.Abstractions;

/// <summary>A value with no JSON kind: stored as its JSON text.</summary>
[TypeOption(typeof(JsonRowValueKinds), "Undefined")]
[ExcludeFromCodeCoverage]
public sealed class UndefinedJsonRowValueKind : JsonRowValueKindBase
{
    /// <summary>Initializes a new instance of the <see cref="UndefinedJsonRowValueKind"/> class.</summary>
    public UndefinedJsonRowValueKind()
        : base("Undefined", JsonValueKind.Undefined)
    {
    }

    /// <inheritdoc />
    public override void Visit(IJsonRowSink sink, string name, JsonElement element, bool nestedFlattened)
    => sink.AddValue(name, element.GetRawText());
}
