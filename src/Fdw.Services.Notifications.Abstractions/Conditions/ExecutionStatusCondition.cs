using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Notifications.Abstractions;

/// <summary>
/// Fires on specific execution status (Failed, Succeeded, etc.).
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(NotificationConditionTypes), "ExecutionStatus", RestrictToCurrentCompilation = true)]
public sealed class ExecutionStatusCondition : NotificationConditionTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ExecutionStatusCondition"/> class.
    /// </summary>
    public ExecutionStatusCondition()
        : base(4, "ExecutionStatus", "flag", "Info")
    {
    }

    /// <inheritdoc />
    public override IGenericResult<bool> Evaluate(NotificationContext context)
    {
        if (string.IsNullOrEmpty(context.Value) || string.IsNullOrEmpty(context.ExecutionStatus))
        {
            return GenericResult<bool>.Success(false);
        }

        var result = string.Equals(context.ExecutionStatus, context.Value, StringComparison.OrdinalIgnoreCase);
        return GenericResult<bool>.Success(context.IsNegated ? !result : result);
    }
}
