using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Fdw.Collections.Attributes;

namespace Fdw.Data.RowSources.Json.Abstractions;

/// <summary>The JSON word false.</summary>
[TypeOption(typeof(JsonRowValueKinds), "False")]
[ExcludeFromCodeCoverage]
public sealed class FalseJsonRowValueKind : JsonRowValueKindBase
{
    /// <summary>Initializes a new instance of the <see cref="FalseJsonRowValueKind"/> class.</summary>
    public FalseJsonRowValueKind()
        : base("False", JsonValueKind.False)
    {
    }

    /// <inheritdoc />
    public override void Visit(IJsonRowSink sink, string name, JsonElement element, bool nestedFlattened)
    => sink.AddValue(name, false);
}
