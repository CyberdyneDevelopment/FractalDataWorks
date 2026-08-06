using Fdw.Collections.Attributes;
using Fdw.Orchestration.Abstractions.TypeCollections.ExecutionStatusOptions;
using ExecutionStatusesCollection = Fdw.Orchestration.Abstractions.TypeCollections.ExecutionStatusOptions.ExecutionStatuses;

namespace Fdw.Orchestration.TypeCollections.ExecutionStatuses;

/// <summary>
/// Execution status indicating the orchestration exceeded its timeout.
/// </summary>
[TypeOption(typeof(ExecutionStatusesCollection), "TimedOut", RestrictToCurrentCompilation = true)]
public sealed class TimedOutStatus : ExecutionStatusBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TimedOutStatus"/> class.
    /// </summary>
    public TimedOutStatus()
        : base(
            id: 8,
            name: "TimedOut",
            isTerminal: true,
            isSuccess: false,
            isFailure: true,
            allowsRetry: true,
            allowsResume: false,
            isInProgress: false)
    {
    }
}
