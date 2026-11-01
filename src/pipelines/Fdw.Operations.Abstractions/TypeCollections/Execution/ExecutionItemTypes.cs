using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Operations.Abstractions.TypeCollections.Execution;

/// <summary>
/// TypeCollection for execution item types defining the execution hierarchy.
/// Workflow → Job → Stage → Step → Task
/// </summary>
/// <remarks>
/// <para>
/// The execution hierarchy allows tracking at multiple granularity levels:
/// <list type="bullet">
/// <item><description>Workflow - Top-level orchestration unit</description></item>
/// <item><description>Job - A discrete unit of work within a workflow</description></item>
/// <item><description>Stage - A phase within a job (e.g., validation, execution, cleanup)</description></item>
/// <item><description>Step - An individual action within a stage</description></item>
/// <item><description>Task - The smallest trackable unit (leaf node)</description></item>
/// </list>
/// </para>
/// </remarks>
[TypeCollection(typeof(ExecutionItemTypeBase), typeof(IExecutionItemType), typeof(ExecutionItemTypes))]
[ExcludeFromCodeCoverage]
public abstract partial class ExecutionItemTypes : TypeCollectionBase<ExecutionItemTypeBase, IExecutionItemType>
{
}

// =============================================================================
// Execution Item Type Options
// =============================================================================