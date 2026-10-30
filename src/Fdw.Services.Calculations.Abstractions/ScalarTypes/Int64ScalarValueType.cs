using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Calculations.Abstractions;

/// <summary>
/// Scalar value type representing a 64-bit integer.
/// </summary>
[TypeOption(typeof(ScalarValueTypes), "Int64")]
[ExcludeFromCodeCoverage]
public sealed class Int64ScalarValueType : ScalarValueTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Int64ScalarValueType"/> class.
    /// </summary>
    public Int64ScalarValueType()
        : base(name: "Int64")
    {
    }

    /// <inheritdoc/>
    /// <remarks>The text is read under the invariant culture, so the same text is the same value on every host.</remarks>
    public override bool IsValidText(string serializedValue) =>
        serializedValue is not null && long.TryParse(serializedValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out _);
}
