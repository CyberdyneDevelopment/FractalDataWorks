using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Projects.Execution;

/// <summary>The pipeline never started because a pipeline it needs failed or was not run.</summary>
[TypeOption(typeof(PipelineRunStates), "NotRun")]
public sealed class NotRunRunState : PipelineRunStateBase
{
    /// <summary>Initializes a new instance of the <see cref="NotRunRunState"/> class.</summary>
    public NotRunRunState()
        : base(5, "NotRun", waitsToStart: false, succeeded: false, blocksDependents: true)
    {
    }
}
