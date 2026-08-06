using System.Diagnostics.CodeAnalysis;
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
        : base(id: 2, name: "Decimal")
    {
    }
}
