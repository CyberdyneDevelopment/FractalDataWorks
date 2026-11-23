using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Orchestration.Pipelines.TypeCollections.ValidationData;

/// <summary>A boolean.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ValidationDataKinds), "Boolean")]
public sealed class BooleanValidationDataKind : ValidationDataKindBase
{
    /// <summary>Initializes a new instance of the <see cref="BooleanValidationDataKind"/> class.</summary>
    public BooleanValidationDataKind()
        : base("Boolean", ["BOOL", "BOOLEAN"])
    {
    }

    /// <inheritdoc />
    public override bool Accepts(string text) => bool.TryParse(text, out _);
}
