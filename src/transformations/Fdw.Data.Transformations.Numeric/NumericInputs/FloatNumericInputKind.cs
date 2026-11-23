using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Data.Transformations;

/// <summary>A float.</summary>
[TypeOption(typeof(NumericInputKinds), "Float")]
[ExcludeFromCodeCoverage]
public sealed class FloatNumericInputKind : NumericInputKindBase
{
    /// <summary>Initializes a new instance of the <see cref="FloatNumericInputKind"/> class.</summary>
    public FloatNumericInputKind()
        : base("Float")
    {
    }

    /// <inheritdoc />
    public override bool Holds(object value) => value is float;

    /// <inheritdoc />
    public override decimal ToDecimal(object value) => Convert.ToDecimal((float)value);
}
