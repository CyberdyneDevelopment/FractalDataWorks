using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Services.Scheduling.Abstractions.OptionTypes.TriggerTypeImplementations;
using Fdw.Services.Scheduling.Abstractions.Results;

namespace Fdw.Services.Scheduling.Abstractions.OptionTypes;

/// <summary>An interval trigger states a positive whole number of minutes, and a start time when it states one it can read.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(StoredTriggerChecks), "Interval")]
public sealed class IntervalStoredTriggerCheck : StoredTriggerCheckBase
{
    /// <summary>Initializes a new instance of the <see cref="IntervalStoredTriggerCheck"/> class.</summary>
    public IntervalStoredTriggerCheck()
        : base("Interval")
    {
    }

    /// <inheritdoc />
    public override IGenericResult Check(IReadOnlyDictionary<string, object> configuration)
    {
        if (!configuration.TryGetValue(Interval.IntervalMinutesKey, out var intervalObj) ||
            !TriggerValueShapes.ReadNarrowInt(intervalObj, out var intervalMinutes) ||
            intervalMinutes <= 0)
        {
            return GenericResult.Failure(SchedulingResultCodes.ByName("IntervalMinutesRequired"));
        }

        if (configuration.TryGetValue(Interval.StartTimeKey, out var startTimeObj) &&
            startTimeObj != null &&
            !TriggerValueShapes.ReadDateTime(startTimeObj, out _))
        {
            return GenericResult.Failure(SchedulingResultCodes.ByName("IntervalStartTimeInvalid"));
        }

        return GenericResult.Success();
    }
}
