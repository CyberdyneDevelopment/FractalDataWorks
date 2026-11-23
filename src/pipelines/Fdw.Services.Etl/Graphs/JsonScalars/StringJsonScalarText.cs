using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Graphs;

/// <summary>A string, as it is.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(JsonScalarTexts), "String")]
public sealed class StringJsonScalarText : JsonScalarTextBase
{
    /// <summary>Initializes a new instance of the <see cref="StringJsonScalarText"/> class.</summary>
    public StringJsonScalarText()
        : base("String", JsonValueKind.String)
    {
    }

    /// <inheritdoc />
    public override string? InvariantText(JsonElement value) => value.GetString();
}
