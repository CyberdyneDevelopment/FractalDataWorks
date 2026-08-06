using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Orchestration.Workflows.Abstractions;
using Fdw.Orchestration.Abstractions;
using Fdw.Results;
using Fdw.Orchestration.Workflows.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Orchestration.Workflows;

/// <summary>
/// Workflow definition. Holds the structural metadata of a workflow (id, name, steps).
/// Execution-time policy (error handling, caching) lives on <see cref="IOrchestrationContext"/>,
/// not on the definition itself.
/// </summary>
public sealed class WorkflowDefinition : IWorkflow
{
    private static readonly ILogger _logger = NullLogger.Instance;

    // IOrchestration members (through IWorkflow inheritance)
    string IOrchestration.OrchestrationId => WorkflowId;
    IReadOnlyList<IOrchestrationStep> IOrchestration.Phases => Steps.Cast<IOrchestrationStep>().ToList();

    // IOrchestration<IWorkflowStep> members
    IReadOnlyList<IWorkflowStep> IOrchestration<IWorkflowStep>.Phases => Steps;

    /// <summary>Getsthe unique workflow identifier.</summary>
    public string WorkflowId { get; init; } = string.Empty;

    /// <summary>Getsthe workflow name.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Getsthe workflow description.</summary>
    public string? Description { get; init; }

    /// <summary>Getsthe workflow version.</summary>
    public string Version { get; init; } = "1.0.0";

    /// <summary>Getsthe workflow steps.</summary>
    public IReadOnlyList<IWorkflowStep> Steps { get; init; } = [];

    /// <summary>Getswhether this workflow is enabled.</summary>
    public bool IsEnabled { get; init; } = true;

    /// <summary>Getsthe workflow tags for categorization.</summary>
    public IReadOnlyList<string> Tags { get; init; } = [];

    /// <summary>Getsthe workflow metadata.</summary>
    public IReadOnlyDictionary<string, object?> Metadata { get; init; } = new Dictionary<string, object?>(StringComparer.Ordinal);

    /// <summary>
    /// Validates the workflow definition.
    /// </summary>
    public Task<IGenericResult> Validate(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(WorkflowId))
            return Task.FromResult<IGenericResult>(GenericResult.Failure(WorkflowLogger.WorkflowIdRequired(_logger)));

        if (string.IsNullOrWhiteSpace(Name))
            return Task.FromResult<IGenericResult>(GenericResult.Failure(WorkflowLogger.WorkflowNameRequired(_logger)));

        if (Steps == null || Steps.Count == 0)
            return Task.FromResult<IGenericResult>(GenericResult.Failure(WorkflowLogger.WorkflowMustHaveSteps(_logger)));

        return Task.FromResult<IGenericResult>(GenericResult.Success());
    }
}
