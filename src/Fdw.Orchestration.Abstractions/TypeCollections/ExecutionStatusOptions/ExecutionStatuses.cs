using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Orchestration.Abstractions.TypeCollections.ExecutionStatusOptions;

/// <summary>
/// TypeCollection for execution statuses.
/// </summary>
/// <remarks>
/// Provides compile-time discovery and O(1) lookup for execution statuses.
/// Source generator creates static properties for each registered execution status.
/// </remarks>
[TypeCollection(typeof(ExecutionStatusBase), typeof(IExecutionStatus), typeof(ExecutionStatuses))]
public sealed partial class ExecutionStatuses : TypeCollectionBase<ExecutionStatusBase, IExecutionStatus>
{
}
