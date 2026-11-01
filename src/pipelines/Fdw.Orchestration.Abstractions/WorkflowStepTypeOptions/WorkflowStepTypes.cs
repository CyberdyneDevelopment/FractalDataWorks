using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Orchestration.Workflows.Abstractions.WorkflowStepTypeOptions;

/// <summary>
/// TypeCollection for workflow step types.
/// </summary>
/// <remarks>
/// Provides compile-time discovery and O(1) lookup for workflow step types.
/// Source generator creates static properties for each registered workflow step type.
/// </remarks>
[TypeCollection(typeof(WorkflowStepTypeBase), typeof(IWorkflowStepType), typeof(WorkflowStepTypes))]
public sealed partial class WorkflowStepTypes : TypeCollectionBase<WorkflowStepTypeBase, IWorkflowStepType>
{
}
