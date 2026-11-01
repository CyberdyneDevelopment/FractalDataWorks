using System.Collections.Generic;
using Fdw.Collections.Attributes;
using Fdw.Services.Etl.Abstractions.Thresholds;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>The level states no thresholds and takes those of the level above it.</summary>
[TypeOption(typeof(WaitThresholdModes), "Inherit")]
public sealed class InheritWaitThresholdMode : WaitThresholdModeBase
{
    /// <inheritdoc/>
    public override IReadOnlyList<string> Levels { get; } = [WaitThresholdOwners.Pipeline, WaitThresholdOwners.Schedule];

    /// <summary>Initializes a new instance of the <see cref="InheritWaitThresholdMode"/> class.</summary>
    public InheritWaitThresholdMode() : base("Inherit", "The level states no thresholds and takes those of the level above it.", inheritsFromParent: true, statesThresholds: false)
    {
    }
}
