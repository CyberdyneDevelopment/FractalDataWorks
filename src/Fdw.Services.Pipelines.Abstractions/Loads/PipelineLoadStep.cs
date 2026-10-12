using System.Collections.Generic;

namespace Fdw.Services.Pipelines.Clients.Abstractions;

/// <summary>One step of a pipeline's ordered load (an entry of <c>loadSteps</c>).</summary>
public sealed class PipelineLoadStep
{
    /// <summary>Gets or sets the 1-based order, equal to the step's position in the list.</summary>
    public int Order { get; set; }

    /// <summary>Gets or sets the name, unique within the pipeline; validation places its problems on this stage.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets a value indicating whether the step runs.</summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>Gets or sets the kind name, from the LoadStepKinds type collection.</summary>
    public string Kind { get; set; } = string.Empty;

    /// <summary>Gets or sets the destination data set.</summary>
    public string DestinationDataSet { get; set; } = string.Empty;

    /// <summary>Gets or sets the step's options; a step that states none states no options (Truncate needs none).</summary>
    public PipelineLoadStepOptions? Options { get; set; }

    /// <summary>Gets or sets the value sources of the step's columns.</summary>
    public IList<PipelineLoadColumnSource> ValueSources { get; set; } = [];
}
