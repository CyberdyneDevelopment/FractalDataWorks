using Fdw.Collections.Attributes;
using Fdw.Orchestration.Abstractions.TypeCollections.ExecutionStatusOptions;
using ExecutionStatusesCollection = Fdw.Orchestration.Abstractions.TypeCollections.ExecutionStatusOptions.ExecutionStatuses;

namespace Fdw.Orchestration.TypeCollections.ExecutionStatuses;

/// <summary>
/// Execution status indicating the orchestration completed successfully.
/// </summary>
[TypeOption(typeof(ExecutionStatusesCollection), "Succeeded", RestrictToCurrentCompilation = true)]
public sealed class SucceededStatus : ExecutionStatusBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SucceededStatus"/> class.
    /// </summary>
    public SucceededStatus()
        : base(
            id: 4,
            name: "Succeeded",
            isTerminal: true,
            isSuccess: true,
            isFailure: false,
            allowsRetry: false,
            allowsResume: false,
            isInProgress: false)
    {
    }
}
