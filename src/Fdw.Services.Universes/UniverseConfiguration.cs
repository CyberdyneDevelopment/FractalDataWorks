using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Services.Universes;

/// <summary>
/// Maps to <c>universe.Universe</c> — a collaborative data project.
/// </summary>
/// <remarks>
/// <para>
/// A Universe is the root of the collaboration hierarchy: members join it, resources belong to
/// it, and the data sets it owns need not be backed by a source yet.
/// </para>
/// <para>
/// Why <see cref="ServiceOptionType"/> is null: a Universe is not dispatched to a factory. It is
/// a configuration record, not a service with pluggable implementations, so it carries no option
/// discriminator — the same as <c>OrchestrationNodeConfiguration</c> and its Stage/Step siblings.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "Universe")]
public partial class UniverseConfiguration : IGenericConfiguration
{
    /// <summary>Gets or sets the durable logical identity, minted by the caller before insert.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the unique universe name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets the configuration section name.</summary>
    public string SectionName => "Universes";

    /// <summary>Gets the structural discriminator.</summary>
    public string ServiceType => "Universe";

    /// <summary>Gets the service option type. Always null — a Universe selects no factory.</summary>
    public string? ServiceOptionType => null;

    /// <summary>Gets or sets the optional display name.</summary>
    public string? DisplayName { get; set; }

    /// <summary>Gets or sets the optional description.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets the question this project exists to answer.</summary>
    public string? Purpose { get; set; }

    /// <summary>Gets or sets the lifecycle status: Draft, Active, Paused or Archived.</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>Gets or sets who can find this project: Private, Discoverable or Open.</summary>
    public string Visibility { get; set; } = string.Empty;

    /// <summary>Gets or sets what happens when someone asks to join: Closed, RequestToJoin or AutoApprove.</summary>
    public string JoinPolicy { get; set; } = string.Empty;

    /// <summary>Gets or sets the owning user.</summary>
    public Guid OwnerUserId { get; set; }

    /// <summary>
    /// Gets or sets the project-wide seed for generated stand-in values. A fixed seed keeps
    /// stand-ins stable, so a saved chart does not change shape on refresh. A field may override it.
    /// </summary>
    public string? StandInSeed { get; set; }

    /// <summary>Gets or sets the optional tenant scope.</summary>
    public Guid? TenantId { get; set; }

    /// <summary>Gets or sets the optional row-level visibility group.</summary>
    public Guid? VisibilityGroupId { get; set; }

    /// <summary>Gets or sets the members of this universe.</summary>
    public IList<UniverseMemberConfiguration> Members { get; set; } = [];

    /// <summary>Gets or sets the resources attached to this universe.</summary>
    public IList<UniverseResourceConfiguration> Resources { get; set; } = [];

    /// <summary>Gets or sets the declared relationships between this universe's data sets.</summary>
    public IList<UniverseRelationshipConfiguration> Relationships { get; set; } = [];

    /// <summary>Gets or sets whether this is the current active version of the row.</summary>
    public bool IsCurrent { get; set; } = true;

    /// <summary>Gets or sets whether the row has been soft-deleted.</summary>
    public bool IsDeleted { get; set; }

    /// <summary>Gets or sets the original creation date from the source system.</summary>
    public DateTimeOffset? SrcCreateDate { get; set; }

    /// <summary>Gets or sets the timestamp when the row was created.</summary>
    public DateTimeOffset CreateDate { get; set; }

    /// <summary>Gets or sets the database user who created the row.</summary>
    public string CreateBy { get; set; } = string.Empty;

    /// <summary>Gets or sets the application user on whose behalf the row was created.</summary>
    public string CreateOnBehalfOf { get; set; } = string.Empty;

    /// <summary>Gets or sets the timestamp when the row was last modified.</summary>
    public DateTimeOffset ModifyDate { get; set; }

    /// <summary>Gets or sets the database user who last modified the row.</summary>
    public string ModifyBy { get; set; } = string.Empty;

    /// <summary>Gets or sets the application user on whose behalf the row was last modified.</summary>
    public string ModifyOnBehalfOf { get; set; } = string.Empty;
}
