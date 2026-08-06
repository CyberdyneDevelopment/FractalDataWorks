using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Notifications.Abstractions;

/// <summary>
/// Fires after N consecutive failures.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(NotificationConditionTypes), "ConsecutiveFailures", RestrictToCurrentCompilation = true)]
public sealed class ConsecutiveFailuresCondition : NotificationConditionTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConsecutiveFailuresCondition"/> class.
    /// </summary>
    public ConsecutiveFailuresCondition()
        : base(2, "ConsecutiveFailures", "error_outline", "Error")
    {
    }

    /// <inheritdoc />
    public override IGenericResult<bool> Evaluate(NotificationContext context)
    {
        if (context.Threshold is null)
        {
            return GenericResult<bool>.Success(false);
        }

        var result = context.ConsecutiveFailures >= context.Threshold.Value;
        return GenericResult<bool>.Success(context.IsNegated ? !result : result);
    }
}
