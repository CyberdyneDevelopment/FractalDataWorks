using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Pipelines.Clients.Models;

/// <summary>
/// TypeCollection for pipeline lifecycle statuses.
/// </summary>
[TypeCollection(typeof(PipelineStatusBase), typeof(IPipelineStatus), typeof(PipelineStatuses))]
[ExcludeFromCodeCoverage]
public abstract partial class PipelineStatuses : TypeCollectionBase<PipelineStatusBase, IPipelineStatus> { }
