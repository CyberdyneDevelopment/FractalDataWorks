using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Calculations.Abstractions.Rounding;

/// <summary>
/// Halves round away from zero.
/// </summary>
[TypeOption(typeof(RoundingModes), "AwayFromZero")]
[ExcludeFromCodeCoverage]
public sealed class AwayFromZeroRoundingMode : RoundingModeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AwayFromZeroRoundingMode"/> class.
    /// </summary>
    public AwayFromZeroRoundingMode()
        : base("AwayFromZero", "Halves round away from zero.", MidpointRounding.AwayFromZero)
    {
    }
}
