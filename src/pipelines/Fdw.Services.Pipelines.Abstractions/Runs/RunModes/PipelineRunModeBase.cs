using Fdw.Collections;

namespace Fdw.Services.Pipelines.Abstractions.Runs;

/// <summary>The base of the ways a pipeline run is carried out (FDW-6131).</summary>
public abstract class PipelineRunModeBase : TypeOptionBase<PipelineRunModeBase>, IPipelineRunMode
{
    /// <summary>Initializes a new instance of the <see cref="PipelineRunModeBase"/> class.</summary>
    /// <param name="name">The run mode's name.</param>
    /// <param name="description">What the run mode does with a run.</param>
    protected PipelineRunModeBase(string name, string description)
        : base(name, name, description, nameof(PipelineRunModes))
    {
    }
}
