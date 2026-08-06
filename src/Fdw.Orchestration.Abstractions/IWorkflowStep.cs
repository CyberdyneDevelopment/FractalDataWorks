using System.Collections.Generic;
using Fdw.Orchestration.Workflows.Abstractions.WorkflowStepTypeOptions;
using Fdw.Orchestration.Abstractions;

namespace Fdw.Orchestration.Workflows.Abstractions;

/// <summary>
/// Represents a step in a workflow.
/// </summary>
/// <remarks>
/// Workflow steps extend orchestration steps with workflow-specific concepts:
/// step types (Pipeline, Decision, Notify, etc.), conditions, and pipeline references.
/// </remarks>
public interface IWorkflowStep : IOrchestrationStep
{
    /// <summary>
    /// Gets the workflow step type.
    /// </summary>
    IWorkflowStepType Type { get; }

    /// <summary>
    /// Gets the pipeline ID to execute (if Type is Pipeline).
    /// </summary>
    string? PipelineId { get; }

    /// <summary>
    /// Gets the condition for running this step.
    /// </summary>
    IWorkflowCondition? Condition { get; }

    /// <summary>
    /// Gets step-specific parameters.
    /// </summary>
    IReadOnlyDictionary<string, object?> Parameters { get; }
}
