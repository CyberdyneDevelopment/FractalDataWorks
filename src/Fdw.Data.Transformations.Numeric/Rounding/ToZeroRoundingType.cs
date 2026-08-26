using System;
using Fdw.Collections.Attributes;

namespace Fdw.Data.Transformations;

/// <summary>Rounds using <see cref="MidpointRounding.ToZero"/>.</summary>
[TypeOption(typeof(RoundingTypes), "ToZero")]
public sealed class ToZeroRoundingType : RoundingTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="ToZeroRoundingType"/> class.</summary>
    public ToZeroRoundingType() : base(3, "ToZero") { }

    /// <inheritdoc/>
    public override decimal Round(decimal value, int precision) =>
        Math.Round(value, precision, MidpointRounding.ToZero);
}
