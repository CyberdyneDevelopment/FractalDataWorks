using Fdw.Collections.Attributes;
using Fdw.Orchestration.Abstractions.TypeCollections.ExecutionStatusOptions;
using ExecutionStatusesCollection = Fdw.Orchestration.Abstractions.TypeCollections.ExecutionStatusOptions.ExecutionStatuses;

namespace Fdw.Orchestration.TypeCollections.ExecutionStatuses;

/// <summary>
/// Execution status indicating the orchestration is currently executing.
/// </summary>
[TypeOption(typeof(ExecutionStatusesCollection), "Running", RestrictToCurrentCompilation = true)]
public sealed class RunningStatus : ExecutionStatusBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RunningStatus"/> class.
    /// </summary>
    public RunningStatus()
        : base(
            id: 2,
            name: "Running",
            isTerminal: false,
            isSuccess: false,
            isFailure: false,
            allowsRetry: false,
            allowsResume: false,
            isInProgress: true)
    {
    }
}
