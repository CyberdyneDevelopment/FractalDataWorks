using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Services.Etl.Abstractions.Thresholds;

namespace Fdw.Services.Etl.Thresholds;

/// <summary>A schedule: it has to exist.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(WaitThresholdOwnerKinds), "Schedule")]
public sealed class ScheduleWaitThresholdOwnerKind : WaitThresholdOwnerKindBase
{
    /// <summary>Initializes a new instance of the <see cref="ScheduleWaitThresholdOwnerKind"/> class.</summary>
    public ScheduleWaitThresholdOwnerKind()
        : base(WaitThresholdOwners.Schedule)
    {
    }

    /// <inheritdoc />
    public override bool OwnerExists(string owner, IReadOnlyDictionary<string, WaitThresholdLayer> pipelines, IReadOnlyDictionary<string, WaitThresholdLayer> schedules) => schedules.ContainsKey(owner);
}
