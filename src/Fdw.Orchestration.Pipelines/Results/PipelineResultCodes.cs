using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Results.Abstractions;

namespace Fdw.Orchestration.Pipelines.Results;

/// <summary>
/// TypeCollection for Pipeline result codes.
/// EventId range: 5700-5799 (within Orchestration domain)
/// </summary>
[TypeCollection(typeof(PipelineResultCodeBase), typeof(IResultCode), typeof(PipelineResultCodes))]
public abstract partial class PipelineResultCodes : TypeCollectionBase<PipelineResultCodeBase, IResultCode>
{
}
