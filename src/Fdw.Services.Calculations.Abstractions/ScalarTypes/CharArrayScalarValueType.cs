using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Calculations.Abstractions;

/// <summary>
/// Scalar value type representing a character array.
/// </summary>
[TypeOption(typeof(ScalarValueTypes), "CharArray")]
[ExcludeFromCodeCoverage]
public sealed class CharArrayScalarValueType : ScalarValueTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CharArrayScalarValueType"/> class.
    /// </summary>
    public CharArrayScalarValueType()
        : base(id: 5, name: "CharArray")
    {
    }
}
