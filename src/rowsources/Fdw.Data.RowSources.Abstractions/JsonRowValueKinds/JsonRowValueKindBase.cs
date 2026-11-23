using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Data.RowSources.Json.Abstractions;

/// <summary>The base of every JSON row value kind.</summary>
[ExcludeFromCodeCoverage]
public abstract class JsonRowValueKindBase : TypeOptionBase<JsonRowValueKindBase>, IJsonRowValueKind
{
    /// <summary>Initializes a new instance of the <see cref="JsonRowValueKindBase"/> class.</summary>
    /// <param name="name">The kind's name.</param>
    /// <param name="valueKind">The JSON value kind this reads.</param>
    protected JsonRowValueKindBase(string name, JsonValueKind valueKind)
        : base(name)
    {
        ValueKind = valueKind;
    }

    /// <inheritdoc />
    [TypeLookup("ByValueKind")]
    public JsonValueKind ValueKind { get; }

    /// <inheritdoc />
    public abstract void Visit(IJsonRowSink sink, string name, JsonElement element, bool nestedFlattened);
}
