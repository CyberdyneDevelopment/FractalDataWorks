using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Projects.Execution;

/// <summary>The states a pipeline of an orchestration node passes through (FDW-4150, was the RunState enum): waiting, running, succeeded, failed or not run.</summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(PipelineRunStateBase), typeof(IPipelineRunState), typeof(PipelineRunStates))]
public abstract partial class PipelineRunStates : TypeCollectionBase<PipelineRunStateBase, IPipelineRunState>
{
    // DO NOT IMPLEMENT BY HAND! Source generator populates ByName/ById/All/NotFound.
}
