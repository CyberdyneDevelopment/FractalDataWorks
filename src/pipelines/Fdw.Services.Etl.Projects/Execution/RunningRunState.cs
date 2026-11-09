using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Projects.Execution;

/// <summary>The pipeline is running.</summary>
[TypeOption(typeof(PipelineRunStates), "Running")]
public sealed class RunningRunState : PipelineRunStateBase
{
    /// <summary>Initializes a new instance of the <see cref="RunningRunState"/> class.</summary>
    public RunningRunState()
        : base(2, "Running", waitsToStart: false, succeeded: false, blocksDependents: false)
    {
    }
}
