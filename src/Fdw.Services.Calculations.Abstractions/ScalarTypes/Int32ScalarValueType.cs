using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Calculations.Abstractions;

/// <summary>
/// Scalar value type representing a 32-bit integer.
/// </summary>
[TypeOption(typeof(ScalarValueTypes), "Int32")]
[ExcludeFromCodeCoverage]
public sealed class Int32ScalarValueType : ScalarValueTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Int32ScalarValueType"/> class.
    /// </summary>
    public Int32ScalarValueType()
        : base(name: "Int32")
    {
    }

    /// <inheritdoc/>
    /// <remarks>The text is read under the invariant culture, so the same text is the same value on every host.</remarks>
    public override bool IsValidText(string serializedValue) =>
        serializedValue is not null && int.TryParse(serializedValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out _);
}
