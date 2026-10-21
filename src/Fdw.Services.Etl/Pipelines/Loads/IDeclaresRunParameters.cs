using System.Collections.Generic;

namespace Fdw.Services.Etl.Pipelines.Loads;

/// <summary>
/// A pipeline configuration that declares run parameters (FDW-1549). Starting a run, and checking the names a schedule or a step
/// would supply, ask this and not a particular kind of pipeline.
/// </summary>
public interface IDeclaresRunParameters
{
    /// <summary>Gets the run parameters the pipeline declares; empty when it declares none.</summary>
    IReadOnlyList<IPipelineRunParameter> DeclaredRunParameters { get; }
}
