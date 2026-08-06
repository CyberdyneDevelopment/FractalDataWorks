using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Scheduling.Abstractions.Results;

/// <summary>
/// Cron expression is required for Cron triggers.
/// </summary>
[TypeOption(typeof(SchedulingResultCodes), "CronExpressionRequired", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class CronExpressionRequiredCode : SchedulingResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CronExpressionRequiredCode"/> class.
    /// </summary>
    public CronExpressionRequiredCode()
        : base(20000, "CronExpressionRequired",
            ResultSeverities.ByName("Warning"),
            "Cron expression is required for Cron triggers",
            isRetryable: false)
    {
    }
}