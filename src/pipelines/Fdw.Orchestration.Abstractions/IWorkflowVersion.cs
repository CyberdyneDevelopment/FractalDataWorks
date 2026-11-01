using System;

namespace Fdw.Orchestration.Workflows.Abstractions;

/// <summary>
/// Represents a workflow version.
/// </summary>
public interface IWorkflowVersion
{
    /// <summary>
    /// Gets the workflow ID.
    /// </summary>
    string WorkflowId { get; }

    /// <summary>
    /// Gets the version.
    /// </summary>
    string Version { get; }

    /// <summary>
    /// Gets when this version was created.
    /// </summary>
    DateTimeOffset CreatedAt { get; }

    /// <summary>
    /// Gets who created this version.
    /// </summary>
    string CreatedBy { get; }

    /// <summary>
    /// Gets the change description.
    /// </summary>
    string? ChangeDescription { get; }

    /// <summary>
    /// Gets whether this is the active version.
    /// </summary>
    bool IsActive { get; }
}
