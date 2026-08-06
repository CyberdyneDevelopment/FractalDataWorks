using Fdw.Collections.Attributes;
using Fdw.Orchestration.Abstractions.TypeCollections.ExecutionStatusOptions;
using ExecutionStatusesCollection = Fdw.Orchestration.Abstractions.TypeCollections.ExecutionStatusOptions.ExecutionStatuses;

namespace Fdw.Orchestration.TypeCollections.ExecutionStatuses;

/// <summary>
/// Execution status indicating the orchestration was cancelled by user or system.
/// </summary>
[TypeOption(typeof(ExecutionStatusesCollection), "Cancelled", RestrictToCurrentCompilation = true)]
public sealed class CancelledStatus : ExecutionStatusBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CancelledStatus"/> class.
    /// </summary>
    public CancelledStatus()
        : base(
            id: 7,
            name: "Cancelled",
            isTerminal: true,
            isSuccess: false,
            isFailure: false,
            allowsRetry: true,
            allowsResume: false,
            isInProgress: false)
    {
    }
}
