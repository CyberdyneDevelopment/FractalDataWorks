using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Services.Pipelines.Abstractions;

namespace Fdw.Services.Pipelines.Webhooks;

/// <summary>
/// A webhook trigger (FDW-1884): the pipeline it starts, whether it is on, the hash of its secret, the roles its runs act with, and
/// the mappings from the call's body to the pipeline's parameters.
/// </summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "WebhookTrigger", ServiceType = "Webhook")]
public sealed partial class WebhookTriggerConfiguration : IWebhookTriggerImplementationConfiguration
{
    /// <summary>Gets or sets the trigger's identity.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the trigger's name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the domain, which is WebhookTrigger.</summary>
    public string Domain { get; set; } = string.Empty;

    /// <summary>Gets or sets the implementation, which is Webhook.</summary>
    public string Implementation { get; set; } = string.Empty;

    /// <inheritdoc />
    public Guid PipelineId { get; set; }

    /// <inheritdoc />
    public string PipelineName { get; set; } = string.Empty;

    /// <inheritdoc />
    public bool IsEnabled { get; set; }

    /// <inheritdoc />
    public string? Description { get; set; }

    /// <inheritdoc />
    public string SecretHash { get; set; } = string.Empty;

    /// <inheritdoc />
    public string SecretPrefix { get; set; } = string.Empty;

    /// <inheritdoc />
    public DateTimeOffset SecretCreatedAt { get; set; }

    /// <inheritdoc />
    public Guid? TenantId { get; set; }

    /// <summary>Gets or sets the roles a run started through the trigger acts with.</summary>
    public IList<WebhookTriggerRunRoleConfiguration> RunRoles { get; set; } = [];

    /// <summary>Gets or sets the mappings from the call's body to the pipeline's parameters, in the order they were written.</summary>
    public IList<WebhookTriggerParameterMapConfiguration> ParameterMaps { get; set; } = [];

    /// <summary>Gets the names of the roles a run acts with.</summary>
    public IReadOnlyList<string> RunRoleNames => [.. RunRoles.Select(role => role.RoleName)];

    /// <summary>Replaces the roles a run acts with.</summary>
    /// <param name="roleNames">The names of the roles.</param>
    public void ReplaceRunRoles(IReadOnlyList<string> roleNames) =>
        RunRoles = [.. roleNames.Select(roleName => new WebhookTriggerRunRoleConfiguration { RoleName = roleName })];
}
