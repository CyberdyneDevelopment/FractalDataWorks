using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Fdw.Collections.Attributes;

namespace Fdw.Web.RestEndpoints.Query;

/// <summary>A string, read as text.</summary>
[TypeOption(typeof(JsonFilterScalarKinds), "String")]
[ExcludeFromCodeCoverage]
public sealed class StringJsonFilterScalarKind : JsonFilterScalarKindBase
{
    /// <summary>Initializes a new instance of the <see cref="StringJsonFilterScalarKind"/> class.</summary>
    public StringJsonFilterScalarKind()
        : base("String", JsonValueKind.String)
    {
    }

    /// <inheritdoc />
    public override object? Read(JsonElement value) => value.GetString();
}
