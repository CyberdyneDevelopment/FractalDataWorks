using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Orchestration.Pipelines.TypeCollections.ValidationData;

/// <summary>Any text.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ValidationDataKinds), "Text")]
public sealed class TextValidationDataKind : ValidationDataKindBase
{
    /// <summary>Initializes a new instance of the <see cref="TextValidationDataKind"/> class.</summary>
    public TextValidationDataKind()
        : base("Text", ["STRING", "TEXT"])
    {
    }

    /// <inheritdoc />
    public override bool Accepts(string text) => true;
}
