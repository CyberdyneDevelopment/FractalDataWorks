using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Web.RestEndpoints.Query;

/// <summary>The base of every JSON input kind.</summary>
[ExcludeFromCodeCoverage]
public abstract class JsonInputKindBase : TypeOptionBase<JsonInputKindBase>, IJsonInputKind
{
    /// <summary>Initializes a new instance of the <see cref="JsonInputKindBase"/> class.</summary>
    /// <param name="name">The kind's name.</param>
    /// <param name="valueKind">The JSON value kind this input kind reads.</param>
    protected JsonInputKindBase(string name, JsonValueKind valueKind)
        : base(name)
    {
        ValueKind = valueKind;
    }

    /// <inheritdoc />
    [TypeLookup("ByValueKind")]
    public JsonValueKind ValueKind { get; }

    /// <inheritdoc />
    public abstract object? Read(JsonElement value);
}
