using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Calculations.Abstractions.Rounding;

/// <summary>
/// Halves round toward positive infinity.
/// </summary>
[TypeOption(typeof(RoundingModes), "ToPositiveInfinity")]
[ExcludeFromCodeCoverage]
public sealed class ToPositiveInfinityRoundingMode : RoundingModeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ToPositiveInfinityRoundingMode"/> class.
    /// </summary>
    public ToPositiveInfinityRoundingMode()
        : base("ToPositiveInfinity", "Halves round toward positive infinity.", (MidpointRounding)4)
    {
        // 4 is MidpointRounding.ToPositiveInfinity: this assembly targets netstandard2.0, whose MidpointRounding does not name it, so the value is
        // stated by number. A test compares every option with MidpointRounding by name on net10.
    }
}
