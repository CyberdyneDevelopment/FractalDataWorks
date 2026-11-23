using System.Text.Json;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Graphs;

/// <summary>Base class for the kinds of single JSON value and how their invariant text is read.</summary>
public abstract class JsonScalarTextBase : TypeOptionBase<JsonScalarTextBase>, IJsonScalarText
{
    /// <summary>Initializes a new instance of the <see cref="JsonScalarTextBase"/> class.</summary>
    /// <param name="name">The option name.</param>
    /// <param name="valueKind">The JSON value kind this option reads.</param>
    protected JsonScalarTextBase(string name, JsonValueKind valueKind)
        : base(name)
    {
        ValueKind = valueKind;
    }

    /// <inheritdoc />
    [TypeLookup("ByValueKind")]
    public JsonValueKind ValueKind { get; }

    /// <inheritdoc />
    public abstract string? InvariantText(JsonElement value);
}
