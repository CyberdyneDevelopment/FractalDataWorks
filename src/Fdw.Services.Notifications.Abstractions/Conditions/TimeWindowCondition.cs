using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Notifications.Abstractions;

/// <summary>
/// Fires if failure occurs within a time window.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(NotificationConditionTypes), "TimeWindow", RestrictToCurrentCompilation = true)]
public sealed class TimeWindowCondition : NotificationConditionTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TimeWindowCondition"/> class.
    /// </summary>
    public TimeWindowCondition()
        : base(6, "TimeWindow", "schedule", "Info")
    {
    }

    /// <inheritdoc />
    public override IGenericResult<bool> Evaluate(NotificationContext context)
    {
        if (context.DurationTicks is null)
        {
            return GenericResult<bool>.Success(false);
        }

        var windowDuration = TimeSpan.FromTicks(context.DurationTicks.Value);
        var result = context.Duration <= windowDuration;
        return GenericResult<bool>.Success(context.IsNegated ? !result : result);
    }
}
