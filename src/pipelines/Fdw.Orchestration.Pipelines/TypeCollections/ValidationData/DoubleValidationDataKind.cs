using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Fdw.Collections.Attributes;

namespace Fdw.Orchestration.Pipelines.TypeCollections.ValidationData;

/// <summary>A floating point number.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ValidationDataKinds), "Double")]
public sealed class DoubleValidationDataKind : ValidationDataKindBase
{
    /// <summary>Initializes a new instance of the <see cref="DoubleValidationDataKind"/> class.</summary>
    public DoubleValidationDataKind()
        : base("Double", ["DOUBLE", "FLOAT"])
    {
    }

    /// <inheritdoc />
    public override bool Accepts(string text) => double.TryParse(text, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out _);
}
