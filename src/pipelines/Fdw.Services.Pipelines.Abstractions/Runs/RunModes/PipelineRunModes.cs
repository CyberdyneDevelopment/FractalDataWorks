using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Pipelines.Abstractions.Runs;

/// <summary>
/// The ways a pipeline run is carried out (FDW-6131). A pipeline type names its run mode (<see cref="IPipelineType.RunModeName"/>), and the runner a host registers for
/// that mode (<see cref="IPipelineRunner"/>) carries the run out. A plain collection, not a service type collection: a run mode has no configuration row.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(PipelineRunModeBase), typeof(IPipelineRunMode), typeof(PipelineRunModes))]
public abstract partial class PipelineRunModes : TypeCollectionBase<PipelineRunModeBase, IPipelineRunMode>
{
    // DO NOT IMPLEMENT BY HAND! Source generator populates ByName/ById/All/NotFound.
}
