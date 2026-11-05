using System;
using Fdw.Collections;

namespace Fdw.Services.Calculations.Abstractions.Rounding;

/// <summary>
/// Base class for the ways a Round node settles a midpoint (CRTP pattern).
/// </summary>
public abstract class RoundingModeBase : TypeOptionBase<RoundingModeBase>, IRoundingMode
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RoundingModeBase"/> class.
    /// </summary>
    /// <param name="name">The mode's name, as a node states it.</param>
    /// <param name="summary">A one-sentence description.</param>
    /// <param name="midpoint">The <see cref="MidpointRounding"/> value the mode stands for.</param>
    protected RoundingModeBase(string name, string summary, MidpointRounding midpoint)
        : base(name)
    {
        Summary = summary;
        Midpoint = midpoint;
    }

    /// <inheritdoc />
    public string Summary { get; }

    /// <inheritdoc />
    public MidpointRounding Midpoint { get; }
}
