using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Orchestration.Abstractions;

namespace Fdw.Orchestration.Workflows.Abstractions;

/// <summary>
/// Represents a workflow (collection of pipelines with dependencies).
/// </summary>
/// <remarks>
/// Workflows extend orchestrations with workflow-specific concepts.
/// Most properties are inherited from IOrchestration.
/// </remarks>
public interface IWorkflow : IOrchestration<IWorkflowStep>
{
    /// <summary>
    /// Gets the workflow ID.
    /// </summary>
    /// <remarks>Alias for OrchestrationId for workflow-specific contexts.</remarks>
    string WorkflowId { get; }

    /// <summary>
    /// Gets the workflow steps.
    /// </summary>
    /// <remarks>Alias for Phases to preserve workflow-specific terminology.</remarks>
    IReadOnlyList<IWorkflowStep> Steps { get; }
}
