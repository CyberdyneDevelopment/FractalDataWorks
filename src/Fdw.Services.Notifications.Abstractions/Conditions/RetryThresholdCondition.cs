using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Notifications.Abstractions;

/// <summary>
/// Fires when retry count exceeds threshold.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(NotificationConditionTypes), "RetryThreshold", RestrictToCurrentCompilation = true)]
public sealed class RetryThresholdCondition : NotificationConditionTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RetryThresholdCondition"/> class.
    /// </summary>
    public RetryThresholdCondition()
        : base(1, "RetryThreshold", "replay", "Warning")
    {
    }

    /// <inheritdoc />
    public override IGenericResult<bool> Evaluate(NotificationContext context)
    {
        if (context.Threshold is null)
        {
            return GenericResult<bool>.Success(false);
        }

        var result = context.RetryCount >= context.Threshold.Value;
        return GenericResult<bool>.Success(context.IsNegated ? !result : result);
    }
}
