using System.Collections.Generic;
using Fdw.Collections.Attributes;
using Fdw.Services.Etl.Abstractions.Thresholds;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>The level states thresholds of its own, and the thresholds of the levels above it do not apply to it.</summary>
[TypeOption(typeof(WaitThresholdModes), "Own")]
public sealed class OwnWaitThresholdMode : WaitThresholdModeBase
{
    /// <inheritdoc/>
    public override IReadOnlyList<string> Levels { get; } = [WaitThresholdOwners.Server, WaitThresholdOwners.Pipeline, WaitThresholdOwners.Schedule];

    /// <summary>Initializes a new instance of the <see cref="OwnWaitThresholdMode"/> class.</summary>
    public OwnWaitThresholdMode() : base("Own", "The level states thresholds of its own, and the thresholds of the levels above it do not apply to it.", inheritsFromParent: false, statesThresholds: true)
    {
    }
}
