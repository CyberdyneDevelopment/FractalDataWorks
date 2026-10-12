using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Results.Abstractions;

namespace Fdw.Orchestration.Abstractions.Results;

/// <summary>
/// TypeCollection for orchestration result codes.
/// Codes use categorized numbers (ORCH prefix): ValidationFailed=20002, ExecutionFailed=70001.
/// </summary>
[TypeCollection(typeof(OrchestrationResultCodeBase), typeof(IResultCode), typeof(OrchestrationResultCodes))]
public abstract partial class OrchestrationResultCodes : TypeCollectionBase<OrchestrationResultCodeBase, IResultCode>
{
}