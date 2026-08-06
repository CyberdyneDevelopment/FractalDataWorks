using Fdw.Collections.Attributes;
using Fdw.Orchestration.Abstractions.TypeCollections.ExecutionStatusOptions;
using ExecutionStatusesCollection = Fdw.Orchestration.Abstractions.TypeCollections.ExecutionStatusOptions.ExecutionStatuses;

namespace Fdw.Orchestration.TypeCollections.ExecutionStatuses;

/// <summary>
/// Execution status indicating the orchestration is paused and can be resumed.
/// </summary>
[TypeOption(typeof(ExecutionStatusesCollection), "Paused", RestrictToCurrentCompilation = true)]
public sealed class PausedStatus : ExecutionStatusBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PausedStatus"/> class.
    /// </summary>
    public PausedStatus()
        : base(
            id: 3,
            name: "Paused",
            isTerminal: false,
            isSuccess: false,
            isFailure: false,
            allowsRetry: false,
            allowsResume: true,
            isInProgress: false)
    {
    }
}
