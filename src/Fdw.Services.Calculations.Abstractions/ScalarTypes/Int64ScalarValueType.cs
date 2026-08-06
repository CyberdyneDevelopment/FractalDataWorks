using System.Diagnostics.CodeAnalysis;
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
        : base(id: 3, name: "Int64")
    {
    }
}
