using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Operations.Abstractions.Results;

/// <summary>
/// Trigger rejected due to concurrent execution.
/// </summary>
[TypeOption(typeof(OperationsResultCodes), "TriggerRejectedConcurrent", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class TriggerRejectedConcurrentCode : OperationsResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TriggerRejectedConcurrentCode"/> class.
    /// </summary>
    public TriggerRejectedConcurrentCode()
        : base(
            41000,
            "TriggerRejectedConcurrent",
            ResultSeverities.ByName("Warning"),
            "Trigger rejected: concurrent execution in progress",
            isRetryable: false)
    {
    }
}