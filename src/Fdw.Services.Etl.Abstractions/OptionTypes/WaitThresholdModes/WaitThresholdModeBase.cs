using System.Collections.Generic;
using Fdw.Collections;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>
/// Base class for the options of <see cref="WaitThresholdModes"/>, using the CRTP pattern.
/// </summary>
public abstract class WaitThresholdModeBase : TypeOptionBase<WaitThresholdModeBase>, IWaitThresholdMode
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WaitThresholdModeBase"/> class.
    /// </summary>
    /// <param name="name">The option name.</param>
    /// <param name="description">What the mode means, as the console shows it beside the option.</param>
    /// <param name="inheritsFromParent">Whether the level takes the thresholds of the level above it.</param>
    /// <param name="statesThresholds">Whether the level states thresholds of its own.</param>
    protected WaitThresholdModeBase(string name, string description, bool inheritsFromParent, bool statesThresholds)
        : base(name, name, description, "WaitThresholdModes")
    {
        InheritsFromParent = inheritsFromParent;
        StatesThresholds = statesThresholds;
        RequiredProperties = statesThresholds ? [ThresholdsProperty] : [];
    }

    /// <summary>The name of the property that carries the thresholds of a level that states some.</summary>
    public const string ThresholdsProperty = "thresholds";

    /// <inheritdoc/>
    public bool InheritsFromParent { get; }

    /// <inheritdoc/>
    public bool StatesThresholds { get; }

    /// <inheritdoc/>
    public abstract IReadOnlyList<string> Levels { get; }

    /// <summary>Gets the properties a level of this mode carries beyond its mode: the thresholds, when it states some. The type-collection route lists it for the console.</summary>
    public IReadOnlyList<string> ExpectedProperties => RequiredProperties;

    /// <summary>Gets the properties a level of this mode must carry.</summary>
    public IReadOnlyList<string> RequiredProperties { get; }
}
