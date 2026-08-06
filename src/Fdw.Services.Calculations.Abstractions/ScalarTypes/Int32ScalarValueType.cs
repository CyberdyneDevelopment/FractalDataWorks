using System.Diagnostics.CodeAnalysis;
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
        : base(id: 1, name: "Int32")
    {
    }
}
