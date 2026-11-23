using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Fdw.Collections.Attributes;

namespace Fdw.Orchestration.Pipelines.TypeCollections.ValidationData;

/// <summary>A decimal number.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ValidationDataKinds), "Decimal")]
public sealed class DecimalValidationDataKind : ValidationDataKindBase
{
    /// <summary>Initializes a new instance of the <see cref="DecimalValidationDataKind"/> class.</summary>
    public DecimalValidationDataKind()
        : base("Decimal", ["DECIMAL", "NUMERIC"])
    {
    }

    /// <inheritdoc />
    public override bool Accepts(string text) => decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out _);
}
