using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Orchestration.Workflows.Abstractions.WorkflowConditionTypeOptions;

/// <summary>
/// TypeCollection for workflow condition types.
/// </summary>
/// <remarks>
/// Provides compile-time discovery and O(1) lookup for workflow condition types.
/// Source generator creates static properties for each registered workflow condition type.
/// </remarks>
[TypeCollection(typeof(WorkflowConditionTypeBase), typeof(IWorkflowConditionType), typeof(WorkflowConditionTypes))]
public sealed partial class WorkflowConditionTypes : TypeCollectionBase<WorkflowConditionTypeBase, IWorkflowConditionType>
{
}
