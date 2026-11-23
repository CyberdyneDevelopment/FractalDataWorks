using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Fdw.Collections.Attributes;

namespace Fdw.Orchestration.Pipelines.TypeCollections.ValidationData;

/// <summary>A whole number that fits an int.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ValidationDataKinds), "Integer")]
public sealed class IntegerValidationDataKind : ValidationDataKindBase
{
    /// <summary>Initializes a new instance of the <see cref="IntegerValidationDataKind"/> class.</summary>
    public IntegerValidationDataKind()
        : base("Integer", ["INT", "INT32", "INTEGER"])
    {
    }

    /// <inheritdoc />
    public override bool Accepts(string text) => int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out _);
}
