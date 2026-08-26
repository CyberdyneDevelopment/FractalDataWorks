using System;
using Fdw.Collections.Attributes;

namespace Fdw.Data.Transformations;

/// <summary>Rounds using <see cref="MidpointRounding.ToNegativeInfinity"/>.</summary>
[TypeOption(typeof(RoundingTypes), "ToNegativeInfinity")]
public sealed class ToNegativeInfinityRoundingType : RoundingTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="ToNegativeInfinityRoundingType"/> class.</summary>
    public ToNegativeInfinityRoundingType() : base(5, "ToNegativeInfinity") { }

    /// <inheritdoc/>
    public override decimal Round(decimal value, int precision) =>
        Math.Round(value, precision, MidpointRounding.ToNegativeInfinity);
}
