using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Projects.Execution;

/// <summary>The pipeline ran and succeeded.</summary>
[TypeOption(typeof(PipelineRunStates), "Succeeded")]
public sealed class SucceededRunState : PipelineRunStateBase
{
    /// <summary>Initializes a new instance of the <see cref="SucceededRunState"/> class.</summary>
    public SucceededRunState()
        : base(3, "Succeeded", waitsToStart: false, succeeded: true, blocksDependents: false)
    {
    }
}
