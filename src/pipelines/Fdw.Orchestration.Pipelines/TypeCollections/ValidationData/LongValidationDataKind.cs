using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Fdw.Collections.Attributes;

namespace Fdw.Orchestration.Pipelines.TypeCollections.ValidationData;

/// <summary>A whole number that fits a long.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ValidationDataKinds), "Long")]
public sealed class LongValidationDataKind : ValidationDataKindBase
{
    /// <summary>Initializes a new instance of the <see cref="LongValidationDataKind"/> class.</summary>
    public LongValidationDataKind()
        : base("Long", ["LONG", "INT64"])
    {
    }

    /// <inheritdoc />
    public override bool Accepts(string text) => long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out _);
}
