using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Scheduling.Abstractions.Results;

/// <summary>
/// Interval minutes must be a positive integer for Interval triggers.
/// </summary>
[TypeOption(typeof(SchedulingResultCodes), "IntervalMinutesRequired", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class IntervalMinutesRequiredCode : SchedulingResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="IntervalMinutesRequiredCode"/> class.
    /// </summary>
    public IntervalMinutesRequiredCode()
        : base(21001, "IntervalMinutesRequired",
            ResultSeverities.ByName("Error"),
            "Interval minutes must be a positive integer for Interval triggers",
            isRetryable: false)
    {
    }
}