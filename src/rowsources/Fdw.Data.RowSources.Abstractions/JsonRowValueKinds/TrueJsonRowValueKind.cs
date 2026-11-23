using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Fdw.Collections.Attributes;

namespace Fdw.Data.RowSources.Json.Abstractions;

/// <summary>The JSON word true.</summary>
[TypeOption(typeof(JsonRowValueKinds), "True")]
[ExcludeFromCodeCoverage]
public sealed class TrueJsonRowValueKind : JsonRowValueKindBase
{
    /// <summary>Initializes a new instance of the <see cref="TrueJsonRowValueKind"/> class.</summary>
    public TrueJsonRowValueKind()
        : base("True", JsonValueKind.True)
    {
    }

    /// <inheritdoc />
    public override void Visit(IJsonRowSink sink, string name, JsonElement element, bool nestedFlattened)
    => sink.AddValue(name, true);
}
