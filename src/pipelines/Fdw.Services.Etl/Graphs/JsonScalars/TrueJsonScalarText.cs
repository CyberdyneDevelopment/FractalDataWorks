using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Graphs;

/// <summary>True.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(JsonScalarTexts), "True")]
public sealed class TrueJsonScalarText : JsonScalarTextBase
{
    /// <summary>Initializes a new instance of the <see cref="TrueJsonScalarText"/> class.</summary>
    public TrueJsonScalarText()
        : base("True", JsonValueKind.True)
    {
    }

    /// <inheritdoc />
    public override string? InvariantText(JsonElement value) => bool.TrueString;
}
