using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Calculations.Abstractions.Rounding;

/// <summary>
/// Halves round toward zero.
/// </summary>
[TypeOption(typeof(RoundingModes), "ToZero")]
[ExcludeFromCodeCoverage]
public sealed class ToZeroRoundingMode : RoundingModeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ToZeroRoundingMode"/> class.
    /// </summary>
    public ToZeroRoundingMode()
        : base("ToZero", "Halves round toward zero.", (MidpointRounding)2)
    {
        // 2 is MidpointRounding.ToZero: this assembly targets netstandard2.0, whose MidpointRounding does not name it, so the value is
        // stated by number. A test compares every option with MidpointRounding by name on net10.
    }
}
