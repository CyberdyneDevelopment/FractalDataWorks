using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Graphs;

/// <summary>A number, as JSON writes it.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(JsonScalarTexts), "Number")]
public sealed class NumberJsonScalarText : JsonScalarTextBase
{
    /// <summary>Initializes a new instance of the <see cref="NumberJsonScalarText"/> class.</summary>
    public NumberJsonScalarText()
        : base("Number", JsonValueKind.Number)
    {
    }

    /// <inheritdoc />
    public override string? InvariantText(JsonElement value) => value.GetRawText();
}
