using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Pipelines.Notifications;

/// <summary>
/// The kinds of pipeline event that cross from a host that serves no hub to the host that does
/// (FDW-3773): StatusChanged, Progress, Completed, TaskStatus, EdgeFlow, Paused and Resumed.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(PipelineEventKindBase), typeof(IPipelineEventKind), typeof(PipelineEventKinds))]
public abstract partial class PipelineEventKinds : TypeCollectionBase<PipelineEventKindBase, IPipelineEventKind>
{
    // DO NOT IMPLEMENT BY HAND! Source generator populates ByName/ById/All/NotFound.
}
