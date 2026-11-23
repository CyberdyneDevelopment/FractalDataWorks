using Fdw.Collections.Attributes;

namespace Fdw.Services.Pipelines.Abstractions.Runs;

/// <summary>The run is recorded, its request kept and queued, and the host's executor runs it when its turn comes.</summary>
[TypeOption(typeof(PipelineRunModes), "Queued")]
public sealed class QueuedPipelineRunMode : PipelineRunModeBase
{
    /// <summary>Initializes a new instance of the <see cref="QueuedPipelineRunMode"/> class.</summary>
    public QueuedPipelineRunMode()
        : base("Queued", "The run is recorded, its request kept and queued, and the host's executor runs it when its turn comes.")
    {
    }
}
