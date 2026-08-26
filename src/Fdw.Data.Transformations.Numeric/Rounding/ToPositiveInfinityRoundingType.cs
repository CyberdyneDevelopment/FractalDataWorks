using System;
using Fdw.Collections.Attributes;

namespace Fdw.Data.Transformations;

/// <summary>Rounds using <see cref="MidpointRounding.ToPositiveInfinity"/>.</summary>
[TypeOption(typeof(RoundingTypes), "ToPositiveInfinity")]
public sealed class ToPositiveInfinityRoundingType : RoundingTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="ToPositiveInfinityRoundingType"/> class.</summary>
    public ToPositiveInfinityRoundingType() : base(4, "ToPositiveInfinity") { }

    /// <inheritdoc/>
    public override decimal Round(decimal value, int precision) =>
        Math.Round(value, precision, MidpointRounding.ToPositiveInfinity);
}
