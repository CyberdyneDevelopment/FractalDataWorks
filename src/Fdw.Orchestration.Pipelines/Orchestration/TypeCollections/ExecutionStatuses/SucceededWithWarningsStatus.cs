using Fdw.Collections.Attributes;
using Fdw.Orchestration.Abstractions.TypeCollections.ExecutionStatusOptions;
using ExecutionStatusesCollection = Fdw.Orchestration.Abstractions.TypeCollections.ExecutionStatusOptions.ExecutionStatuses;

namespace Fdw.Orchestration.TypeCollections.ExecutionStatuses;

/// <summary>
/// Execution status indicating the orchestration completed with non-fatal warnings.
/// </summary>
[TypeOption(typeof(ExecutionStatusesCollection), "SucceededWithWarnings", RestrictToCurrentCompilation = true)]
public sealed class SucceededWithWarningsStatus : ExecutionStatusBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SucceededWithWarningsStatus"/> class.
    /// </summary>
    public SucceededWithWarningsStatus()
        : base(
            id: 5,
            name: "SucceededWithWarnings",
            isTerminal: true,
            isSuccess: true,
            isFailure: false,
            allowsRetry: false,
            allowsResume: false,
            isInProgress: false,
            hasWarnings: true)
    {
    }
}
