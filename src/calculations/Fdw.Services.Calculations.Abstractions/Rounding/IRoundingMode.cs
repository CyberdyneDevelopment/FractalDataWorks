using System;
using Fdw.Collections;

namespace Fdw.Services.Calculations.Abstractions.Rounding;

/// <summary>
/// How a Round node settles a number that lies exactly between two candidates: to even, away from zero, toward zero,
/// toward negative infinity or toward positive infinity.
/// </summary>
public interface IRoundingMode : ITypeOption<int, RoundingModeBase>
{
    /// <summary>Gets a one-sentence description of the mode.</summary>
    string Summary { get; }

    /// <summary>Gets the <see cref="MidpointRounding"/> value the mode stands for; read only where <see cref="Math.Round(decimal, int, MidpointRounding)"/> is called.</summary>
    MidpointRounding Midpoint { get; }
}
