using Fdw.Collections.Attributes;

namespace Fdw.Services.Pipelines.Abstractions.Runs;

/// <summary>The run is carried out at once, in the host that took the request, within its stated limits.</summary>
[TypeOption(typeof(PipelineRunModes), "AtOnce")]
public sealed class AtOncePipelineRunMode : PipelineRunModeBase
{
    /// <summary>Initializes a new instance of the <see cref="AtOncePipelineRunMode"/> class.</summary>
    public AtOncePipelineRunMode()
        : base("AtOnce", "The run is carried out at once, in the host that took the request, within its stated limits.")
    {
    }
}
