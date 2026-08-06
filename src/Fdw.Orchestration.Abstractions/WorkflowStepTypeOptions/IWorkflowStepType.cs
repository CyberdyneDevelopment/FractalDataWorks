using Fdw.Collections;

namespace Fdw.Orchestration.Workflows.Abstractions.WorkflowStepTypeOptions;

/// <summary>
/// Interface for workflow step types.
/// Extends ITypeOption to enable TypeCollection discovery.
/// </summary>
public interface IWorkflowStepType : ITypeOption<int, WorkflowStepTypeBase>
{
    /// <summary>
    /// Gets a value indicating whether this step type executes a pipeline.
    /// </summary>
    bool ExecutesPipeline { get; }

    /// <summary>
    /// Gets a value indicating whether this step type supports parallelism.
    /// </summary>
    bool SupportsParallelism { get; }
}
