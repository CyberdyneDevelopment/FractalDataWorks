using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Services.Pipelines.Abstractions;

namespace Fdw.Services.Pipelines;

/// <summary>
/// General header configuration for pipeline services representing the pipe.Pipeline parent table.
/// The pipeline KIND lives in <see cref="ServiceOptionType"/> (e.g. "Etl") and the kind-specific
/// fields live on the <see cref="Configuration"/> typed body (e.g. <c>EtlPipelineConfiguration</c>).
/// </summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "Pipeline")]
public partial class PipelineConfiguration : IPipelineConfiguration
{
    /// <summary>
    /// Gets or sets the unique identifier for this pipeline.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the name of this pipeline for lookup and display.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets the section name for configuration binding.
    /// </summary>
    public string SectionName => "Pipelines";

    /// <summary>
    /// Gets the service type (domain) - always "Pipeline" for this configuration.
    /// </summary>
    public string ServiceType => "Pipeline";

    /// <summary>
    /// Gets or sets the service option type — the pipeline KIND discriminator (e.g., "Etl").
    /// Drives typed-body dispatch to the kind body (<c>EtlPipelineConfiguration</c>) via the registered
    /// typed providers. No <c>[ValuesFrom]</c> static value-list: kinds are sourced from the registered
    /// typed providers, mirroring the engine discriminator on <c>EtlPipelineConfiguration</c>.
    /// </summary>
    public string? ServiceOptionType { get; set; }

    /// <summary>
    /// Gets the pipeline kind name. Alias for <see cref="ServiceOptionType"/> for domain convenience.
    /// </summary>
    public string? PipelineType => ServiceOptionType;

    /// <summary>Gets or sets the optional description of this pipeline.</summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets whether this pipeline is scheduled for automatic execution.
    /// </summary>
    public bool IsScheduled { get; set; }

    /// <summary>
    /// Gets or sets the schedule identifier for automatic execution.
    /// </summary>
    public Guid? ScheduleId { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the organization that owns this pipeline.
    /// </summary>
    /// <remarks>
    /// Scopes real-time status broadcasts to the owning org's firehose group
    /// (<c>org:{OrgId}:pipeline-updates</c>): the broadcaster targets that group and a connection joins
    /// only its own org's group (from the JWT <c>org_id</c> claim). Null means the pipeline has no
    /// owning org and therefore appears on no org firehose — its execution-scoped groups
    /// (<c>execution:{id}</c>/<c>pipeline:{name}</c>) still deliver to explicit subscribers.
    /// </remarks>
    public Guid? OrgId { get; set; }

    /// <summary>
    /// Gets or sets the kind typed-body configuration that corresponds to <see cref="ServiceOptionType"/>
    /// (e.g. <c>EtlPipelineConfiguration</c> for kind "Etl"). The keystone cascade persists this typed-body
    /// row alongside the parent on write and composes it on read.
    /// </summary>
    public IPipelineImplementationConfiguration? Configuration { get; set; }

    /// <inheritdoc />
    public IGenericConfiguration? ServiceDispatchBody => Configuration;
}
