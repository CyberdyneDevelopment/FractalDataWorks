using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Calculations.Abstractions;

/// <summary>
/// Scalar value type representing a Unicode string.
/// </summary>
[TypeOption(typeof(ScalarValueTypes), "String")]
[ExcludeFromCodeCoverage]
public sealed class StringScalarValueType : ScalarValueTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StringScalarValueType"/> class.
    /// </summary>
    public StringScalarValueType()
        : base(id: 4, name: "String")
    {
    }
}
