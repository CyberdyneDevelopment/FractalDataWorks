using Fdw.Collections.Attributes;
using Fdw.Orchestration.Abstractions.TypeCollections.ExecutionStatusOptions;
using ExecutionStatusesCollection = Fdw.Orchestration.Abstractions.TypeCollections.ExecutionStatusOptions.ExecutionStatuses;

namespace Fdw.Orchestration.TypeCollections.ExecutionStatuses;

/// <summary>
/// Execution status indicating the orchestration is waiting to execute.
/// </summary>
[TypeOption(typeof(ExecutionStatusesCollection), "Queued", RestrictToCurrentCompilation = true)]
public sealed class QueuedStatus : ExecutionStatusBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="QueuedStatus"/> class.
    /// </summary>
    public QueuedStatus()
        : base(
            id: 1,
            name: "Queued",
            isTerminal: false,
            isSuccess: false,
            isFailure: false,
            allowsRetry: false,
            allowsResume: false,
            isInProgress: false)
    {
    }
}
