using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Fdw.Collections.Attributes;

namespace Fdw.Data.RowSources.Json.Abstractions;

/// <summary>The JSON word null: an empty field.</summary>
[TypeOption(typeof(JsonRowValueKinds), "Null")]
[ExcludeFromCodeCoverage]
public sealed class NullJsonRowValueKind : JsonRowValueKindBase
{
    /// <summary>Initializes a new instance of the <see cref="NullJsonRowValueKind"/> class.</summary>
    public NullJsonRowValueKind()
        : base("Null", JsonValueKind.Null)
    {
    }

    /// <inheritdoc />
    public override void Visit(IJsonRowSink sink, string name, JsonElement element, bool nestedFlattened)
    => sink.AddValue(name, null);
}
