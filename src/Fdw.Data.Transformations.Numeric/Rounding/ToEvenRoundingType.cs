using System;
using Fdw.Collections.Attributes;

namespace Fdw.Data.Transformations;

/// <summary>Rounds using <see cref="MidpointRounding.ToEven"/>.</summary>
[TypeOption(typeof(RoundingTypes), "ToEven")]
public sealed class ToEvenRoundingType : RoundingTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="ToEvenRoundingType"/> class.</summary>
    public ToEvenRoundingType() : base(2, "ToEven") { }

    /// <inheritdoc/>
    public override decimal Round(decimal value, int precision) =>
        Math.Round(value, precision, MidpointRounding.ToEven);
}
