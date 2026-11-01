using System.Collections.Generic;
using Fdw.Collections.Attributes;
using Fdw.Services.Etl.Abstractions.Thresholds;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>The level states that it has no thresholds, and no alert is raised for it.</summary>
[TypeOption(typeof(WaitThresholdModes), "None")]
public sealed class NoneWaitThresholdMode : WaitThresholdModeBase
{
    /// <inheritdoc/>
    public override IReadOnlyList<string> Levels { get; } = [WaitThresholdOwners.Server, WaitThresholdOwners.Pipeline, WaitThresholdOwners.Schedule];

    /// <summary>Initializes a new instance of the <see cref="NoneWaitThresholdMode"/> class.</summary>
    public NoneWaitThresholdMode() : base("None", "The level states that it has no thresholds, and no alert is raised for it.", inheritsFromParent: false, statesThresholds: false)
    {
    }
}
