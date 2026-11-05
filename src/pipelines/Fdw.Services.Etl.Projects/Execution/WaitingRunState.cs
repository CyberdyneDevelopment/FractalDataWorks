using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Projects.Execution;

/// <summary>The pipeline has not started: it starts once every pipeline it needs has succeeded.</summary>
[TypeOption(typeof(PipelineRunStates), "Waiting")]
public sealed class WaitingRunState : PipelineRunStateBase
{
    /// <summary>Initializes a new instance of the <see cref="WaitingRunState"/> class.</summary>
    public WaitingRunState()
        : base(1, "Waiting", waitsToStart: true, succeeded: false, blocksDependents: false)
    {
    }
}
