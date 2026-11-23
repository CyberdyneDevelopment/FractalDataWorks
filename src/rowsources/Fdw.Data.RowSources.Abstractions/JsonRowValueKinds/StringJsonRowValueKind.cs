using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Fdw.Collections.Attributes;

namespace Fdw.Data.RowSources.Json.Abstractions;

/// <summary>A string: the text.</summary>
[TypeOption(typeof(JsonRowValueKinds), "String")]
[ExcludeFromCodeCoverage]
public sealed class StringJsonRowValueKind : JsonRowValueKindBase
{
    /// <summary>Initializes a new instance of the <see cref="StringJsonRowValueKind"/> class.</summary>
    public StringJsonRowValueKind()
        : base("String", JsonValueKind.String)
    {
    }

    /// <inheritdoc />
    public override void Visit(IJsonRowSink sink, string name, JsonElement element, bool nestedFlattened)
    => sink.AddValue(name, element.GetString());
}
