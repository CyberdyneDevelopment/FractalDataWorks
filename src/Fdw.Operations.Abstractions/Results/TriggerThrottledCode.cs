using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Operations.Abstractions.Results;

/// <summary>
/// Trigger throttled, retry after specified time.
/// </summary>
[TypeOption(typeof(OperationsResultCodes), "TriggerThrottled", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class TriggerThrottledCode : OperationsResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TriggerThrottledCode"/> class.
    /// </summary>
    public TriggerThrottledCode()
        : base(
            81000,
            "TriggerThrottled",
            ResultSeverities.ByName("Warning"),
            "Trigger throttled, retry after {RetryAfter}",
            isRetryable: true)
    {
    }
}