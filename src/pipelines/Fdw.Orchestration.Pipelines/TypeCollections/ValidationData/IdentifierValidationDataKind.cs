using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Orchestration.Pipelines.TypeCollections.ValidationData;

/// <summary>A GUID.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ValidationDataKinds), "Identifier")]
public sealed class IdentifierValidationDataKind : ValidationDataKindBase
{
    /// <summary>Initializes a new instance of the <see cref="IdentifierValidationDataKind"/> class.</summary>
    public IdentifierValidationDataKind()
        : base("Identifier", ["GUID", "UUID"])
    {
    }

    /// <inheritdoc />
    public override bool Accepts(string text) => Guid.TryParse(text, out _);
}
