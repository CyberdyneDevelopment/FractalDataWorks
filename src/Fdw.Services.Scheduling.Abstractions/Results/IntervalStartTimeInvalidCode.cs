using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Scheduling.Abstractions.Results;

/// <summary>
/// Start time must be a valid DateTime if provided for Interval triggers.
/// </summary>
[TypeOption(typeof(SchedulingResultCodes), "IntervalStartTimeInvalid", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class IntervalStartTimeInvalidCode : SchedulingResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="IntervalStartTimeInvalidCode"/> class.
    /// </summary>
    public IntervalStartTimeInvalidCode()
        : base(21002, "IntervalStartTimeInvalid",
            ResultSeverities.ByName("Error"),
            "Start time must be a valid DateTime if provided for Interval triggers",
            isRetryable: false)
    {
    }
}