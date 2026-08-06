using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Scheduling.Abstractions.Results;

/// <summary>
/// Unknown trigger type.
/// </summary>
[TypeOption(typeof(SchedulingResultCodes), "UnknownTriggerType", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class UnknownTriggerTypeCode : SchedulingResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UnknownTriggerTypeCode"/> class.
    /// </summary>
    public UnknownTriggerTypeCode()
        : base(21005, "UnknownTriggerType",
            ResultSeverities.ByName("Warning"),
            "Unknown trigger type: {TriggerType}",
            isRetryable: false)
    {
    }
}