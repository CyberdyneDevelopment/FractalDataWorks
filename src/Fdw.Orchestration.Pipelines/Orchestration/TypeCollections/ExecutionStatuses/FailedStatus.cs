using Fdw.Collections.Attributes;
using Fdw.Orchestration.Abstractions.TypeCollections.ExecutionStatusOptions;
using ExecutionStatusesCollection = Fdw.Orchestration.Abstractions.TypeCollections.ExecutionStatusOptions.ExecutionStatuses;

namespace Fdw.Orchestration.TypeCollections.ExecutionStatuses;

/// <summary>
/// Execution status indicating the orchestration failed.
/// </summary>
[TypeOption(typeof(ExecutionStatusesCollection), "Failed", RestrictToCurrentCompilation = true)]
public sealed class FailedStatus : ExecutionStatusBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FailedStatus"/> class.
    /// </summary>
    public FailedStatus()
        : base(
            id: 6,
            name: "Failed",
            isTerminal: true,
            isSuccess: false,
            isFailure: true,
            allowsRetry: true,
            allowsResume: false,
            isInProgress: false)
    {
    }
}
