using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Orchestration.Workflows.Abstractions.WorkflowExecutionStatusOptions;

/// <summary>
/// TypeCollection for workflow execution status values.
/// </summary>
/// <remarks>
/// Provides compile-time discovery and O(1) lookup for workflow execution statuses.
/// Source generator creates static properties for each registered workflow execution status.
/// </remarks>
[TypeCollection(typeof(WorkflowExecutionStatusBase), typeof(IWorkflowExecutionStatus), typeof(WorkflowExecutionStatuses))]
public sealed partial class WorkflowExecutionStatuses : TypeCollectionBase<WorkflowExecutionStatusBase, IWorkflowExecutionStatus>
{
}
