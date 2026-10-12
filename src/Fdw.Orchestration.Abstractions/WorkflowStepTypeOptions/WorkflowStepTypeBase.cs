using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;

namespace Fdw.Orchestration.Workflows.Abstractions.WorkflowStepTypeOptions;

/// <summary>
/// Base class for workflow step types.
/// </summary>
/// <ExcludeFromCoverageReason>TypeOption base class - no logic to test</ExcludeFromCoverageReason>
[ExcludeFromCodeCoverage]
public abstract class WorkflowStepTypeBase : TypeOptionBase<WorkflowStepTypeBase>, IWorkflowStepType
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WorkflowStepTypeBase"/> class.
    /// </summary>
    /// <param name="name">The name of this workflow step type.</param>
    /// <param name="executesPipeline">Whether this step type executes a pipeline.</param>
    /// <param name="supportsParallelism">Whether this step type supports parallelism.</param>
    protected WorkflowStepTypeBase(string name, bool executesPipeline, bool supportsParallelism)
        : base(name)
    {
        ExecutesPipeline = executesPipeline;
        SupportsParallelism = supportsParallelism;
    }

    /// <inheritdoc />
    public bool ExecutesPipeline { get; }

    /// <inheritdoc />
    public bool SupportsParallelism { get; }
}
