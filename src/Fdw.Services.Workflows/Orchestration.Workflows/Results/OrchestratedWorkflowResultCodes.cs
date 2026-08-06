using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Results.Abstractions;

namespace Fdw.Orchestration.Workflows.Results;

/// <summary>
/// TypeCollection for Orchestration.Workflows result codes.
/// EventId range: 5700-5799 (Orchestration.Pipelines/Workflows domain)
/// </summary>
// Why: renamed from WorkflowResultCodes to OrchestratedWorkflowResultCodes to avoid a TypeCollectionGenerator
// hintName collision with Fdw.Services.Workflows.Results.WorkflowResultCodes — both classes
// lived in separate assemblies before the Orchestration.Workflows fold and the generator only uses the
// short class name as the hintName, not the fully-qualified name.
[TypeCollection(typeof(WorkflowResultCodeBase), typeof(IResultCode), typeof(OrchestratedWorkflowResultCodes))]
public abstract partial class OrchestratedWorkflowResultCodes : TypeCollectionBase<WorkflowResultCodeBase, IResultCode>
{
}
