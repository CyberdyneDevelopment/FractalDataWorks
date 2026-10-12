using System.Collections.Generic;

namespace Fdw.Services.Etl.Pipelines.Loads;

/// <summary>
/// A pipeline implementation that states how it loads its destination (FDW-1110): a load mode or ordered load steps,
/// and the run parameters it declares. The API reads and writes the load definition through it, so no endpoint names an
/// engine's configuration type.
/// </summary>
public interface IPipelineLoadConfiguration
{
    /// <summary>Gets the load mode name, or <see langword="null"/> when the pipeline states load steps instead.</summary>
    string? LoadMode { get; }

    /// <summary>Gets the load rows: the pipeline's own load (step order <see langword="null"/>) or its load steps.</summary>
    IList<PipelineLoadStepConfiguration>? LoadSteps { get; }

    /// <summary>Gets the run parameters the pipeline declares.</summary>
    IList<PipelineRunParameterConfiguration>? RunParameters { get; }
}
