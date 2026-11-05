using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Projects.Execution;

/// <summary>The pipeline ran and failed.</summary>
[TypeOption(typeof(PipelineRunStates), "Failed")]
public sealed class FailedRunState : PipelineRunStateBase
{
    /// <summary>Initializes a new instance of the <see cref="FailedRunState"/> class.</summary>
    public FailedRunState()
        : base(4, "Failed", waitsToStart: false, succeeded: false, blocksDependents: true)
    {
    }
}
