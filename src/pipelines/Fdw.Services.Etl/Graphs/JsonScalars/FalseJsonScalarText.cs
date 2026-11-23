using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Graphs;

/// <summary>False.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(JsonScalarTexts), "False")]
public sealed class FalseJsonScalarText : JsonScalarTextBase
{
    /// <summary>Initializes a new instance of the <see cref="FalseJsonScalarText"/> class.</summary>
    public FalseJsonScalarText()
        : base("False", JsonValueKind.False)
    {
    }

    /// <inheritdoc />
    public override string? InvariantText(JsonElement value) => bool.FalseString;
}
