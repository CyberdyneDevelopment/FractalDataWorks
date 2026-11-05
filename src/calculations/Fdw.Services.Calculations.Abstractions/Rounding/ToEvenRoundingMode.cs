using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Calculations.Abstractions.Rounding;

/// <summary>
/// Halves round to the nearest even number (banker's rounding).
/// </summary>
[TypeOption(typeof(RoundingModes), "ToEven")]
[ExcludeFromCodeCoverage]
public sealed class ToEvenRoundingMode : RoundingModeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ToEvenRoundingMode"/> class.
    /// </summary>
    public ToEvenRoundingMode()
        : base("ToEven", "Halves round to the nearest even number (banker's rounding).", MidpointRounding.ToEven)
    {
    }
}
