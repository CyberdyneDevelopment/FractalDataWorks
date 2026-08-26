using System;
using Fdw.Collections.Attributes;

namespace Fdw.Data.Transformations;

/// <summary>Rounds using <see cref="MidpointRounding.AwayFromZero"/>.</summary>
[TypeOption(typeof(RoundingTypes), "AwayFromZero")]
public sealed class AwayFromZeroRoundingType : RoundingTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="AwayFromZeroRoundingType"/> class.</summary>
    public AwayFromZeroRoundingType() : base(1, "AwayFromZero") { }

    /// <inheritdoc/>
    public override decimal Round(decimal value, int precision) =>
        Math.Round(value, precision, MidpointRounding.AwayFromZero);
}
