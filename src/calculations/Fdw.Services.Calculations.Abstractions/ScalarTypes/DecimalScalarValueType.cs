using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Calculations.Abstractions;

/// <summary>
/// Scalar value type representing a decimal number.
/// </summary>
[TypeOption(typeof(ScalarValueTypes), "Decimal")]
[ExcludeFromCodeCoverage]
public sealed class DecimalScalarValueType : ScalarValueTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DecimalScalarValueType"/> class.
    /// </summary>
    public DecimalScalarValueType()
        : base(name: "Decimal")
    {
    }

    /// <inheritdoc/>
    /// <remarks>The text is read under the invariant culture, so the same text is the same value on every host.</remarks>
    public override bool IsValidText(string serializedValue) =>
        serializedValue is not null && decimal.TryParse(serializedValue, NumberStyles.Number, CultureInfo.InvariantCulture, out _);
}
