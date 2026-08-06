using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Services.Abstractions;

namespace Fdw.Services.Workflows;

/// <summary>
/// Base configuration class for all workflow types.
/// Generates the parent table <c>workflow.Workflow</c> which contains core fields shared by all workflow types.
/// </summary>
/// <remarks>
/// <para>
/// This class serves two purposes:
/// <list type="bullet">
/// <item><description>As a header configuration for <c>IOptionsSnapshot&lt;List&lt;WorkflowConfiguration&gt;&gt;</c> lookups</description></item>
/// <item><description>As the base class for type-specific configurations (if workflow variants are added)</description></item>
/// </list>
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration( ServiceCategory = "Workflow")]
public partial class WorkflowConfiguration : IGenericConfiguration
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WorkflowConfiguration"/> class.
    /// Default constructor for IOptions binding and header lookups.
    /// </summary>
    public WorkflowConfiguration() : this("Workflow", null, "Workflows")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WorkflowConfiguration"/> class.
    /// Protected constructor for derived classes to set their type identity.
    /// </summary>
    /// <param name="serviceType">The service type (domain) - always "Workflow".</param>
    /// <param name="serviceOptionType">The service option type (e.g., "Stateful", "Saga").</param>
    /// <param name="sectionName">The configuration section name for binding.</param>
    protected WorkflowConfiguration(string serviceType, string? serviceOptionType, string sectionName)
    {
        ServiceType = serviceType;
        ServiceOptionType = serviceOptionType;
        SectionName = sectionName;
    }

    /// <summary>
    /// Gets or sets the unique identifier for this workflow.
    /// </summary>
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>
    /// Gets or sets the name of this workflow for lookup and display.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the section name for configuration binding.
    /// </summary>
    public string SectionName { get; set; }

    /// <summary>
    /// Gets or sets the service type (domain) - always "Workflow" for this configuration.
    /// </summary>
    public string ServiceType { get; set; }

    /// <summary>
    /// Gets or sets the service option type (e.g., "Stateful", "Saga").
    /// </summary>
    public string? ServiceOptionType { get; set; }

    /// <summary>
    /// Gets the workflow type name. Alias for <see cref="ServiceOptionType"/>.
    /// </summary>
    public string? WorkflowType => ServiceOptionType;

    /// <inheritdoc />
    public IServiceLifetime Lifetime { get; set; } = ServiceLifetimes.Scoped;

    /// <inheritdoc />
    public string? SecretManagerName { get; set; }

    /// <inheritdoc />
    public string? SecretKeyName { get; set; }

    /// <inheritdoc />
    public int MaxConcurrentExecutions { get; set; } = 10;

    /// <inheritdoc />
    public TimeSpan DefaultExecutionTimeout { get; set; } = TimeSpan.FromHours(1);

    /// <inheritdoc />
    public bool EnableCompensation { get; set; }

    /// <inheritdoc />
    public bool EnablePersistence { get; set; }

    /// <inheritdoc />
    public string? PersistenceStorageConnectionName { get; set; }

    /// <inheritdoc />
    public bool EnableTracing { get; set; }

    /// <summary>
    /// Gets or sets the optional description of this workflow.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the initial state identifier for workflow execution.
    /// </summary>
    public Guid? StartStateId { get; set; }

    /// <summary>
    /// Gets or sets the error state identifier for failed workflow transitions.
    /// </summary>
    public Guid? ErrorStateId { get; set; }

    /// <summary>
    /// Gets or sets the timeout in seconds for individual workflow steps.
    /// </summary>
    public int? TimeoutSeconds { get; set; }

    /// <summary>
    /// Gets or sets the retry policy name for failed step recovery.
    /// </summary>
    public string? RetryPolicy { get; set; }

}
