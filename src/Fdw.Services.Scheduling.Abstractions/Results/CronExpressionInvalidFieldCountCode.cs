using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Scheduling.Abstractions.Results;

/// <summary>
/// Cron expression must have at least 5 fields.
/// </summary>
[TypeOption(typeof(SchedulingResultCodes), "CronExpressionInvalidFieldCount", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class CronExpressionInvalidFieldCountCode : SchedulingResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CronExpressionInvalidFieldCountCode"/> class.
    /// </summary>
    public CronExpressionInvalidFieldCountCode()
        : base(20001, "CronExpressionInvalidFieldCount",
            ResultSeverities.ByName("Error"),
            "Cron expression must have at least 5 fields",
            isRetryable: false)
    {
    }
}