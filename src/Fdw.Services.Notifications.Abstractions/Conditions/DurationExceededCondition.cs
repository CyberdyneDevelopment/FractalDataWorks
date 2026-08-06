using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Notifications.Abstractions;

/// <summary>
/// Fires if execution exceeds expected duration.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(NotificationConditionTypes), "DurationExceeded", RestrictToCurrentCompilation = true)]
public sealed class DurationExceededCondition : NotificationConditionTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DurationExceededCondition"/> class.
    /// </summary>
    public DurationExceededCondition()
        : base(3, "DurationExceeded", "timer_off", "Warning")
    {
    }

    /// <inheritdoc />
    public override IGenericResult<bool> Evaluate(NotificationContext context)
    {
        if (context.DurationTicks is null)
        {
            return GenericResult<bool>.Success(false);
        }

        var threshold = TimeSpan.FromTicks(context.DurationTicks.Value);
        var result = context.Duration > threshold;
        return GenericResult<bool>.Success(context.IsNegated ? !result : result);
    }
}
