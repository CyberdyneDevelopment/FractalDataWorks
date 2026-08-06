using System;
using Fdw.Configuration;
using Fdw.Orchestration.Workflows.Abstractions;

namespace Fdw.Orchestration.Workflows;

/// <summary>
/// Concrete parent configuration for workflows.
/// Per the polymorphic-config pattern, this is the parent header; variant-specific
/// workflow types extend this class to add their typed body fields.
/// </summary>
public partial class WorkflowConfiguration : IGenericConfiguration
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WorkflowConfiguration"/> class.
    /// </summary>
    public WorkflowConfiguration()
    {
        Name = string.Empty;
        SectionName = "Workflows";
        ServiceType = "Workflow";
    }

    /// <inheritdoc/>
    public Guid Id { get; set; }

    /// <inheritdoc/>
    public string Name { get; set; }

    /// <inheritdoc/>
    public string SectionName { get; set; }

    /// <inheritdoc/>
    public string ServiceType { get; set; }

    /// <inheritdoc/>
    public string? ServiceOptionType { get; set; }

    /// <summary>Gets or sets the workflow timeout.</summary>
    public TimeSpan? WorkflowTimeout { get; set; }

    /// <summary>Gets or sets whether to continue on failure.</summary>
    public bool ContinueOnFailure { get; set; }

    /// <summary>Gets or sets the maximum number of steps that can execute concurrently.</summary>
    public int MaxConcurrentSteps { get; set; } = 1;

    /// <summary>Gets or sets whether to enable compensation on failure.</summary>
    public bool EnableCompensation { get; set; }

    /// <summary>Gets or sets the notification settings.</summary>
    public IWorkflowNotificationSettings? NotificationSettings { get; set; }
}
