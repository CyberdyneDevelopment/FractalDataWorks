using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Services.Universes;

/// <summary>
/// Maps to <c>universe.SavedView</c> — a stored visualisation of a data set.
/// </summary>
/// <remarks>
/// <para>
/// Why this is a first-class entity rather than session state: lineage points at it. Tracing a
/// value lists the saved views that read it as downstream consumers, which only works if a saved
/// view has a durable identity. <c>settings.SessionState</c> is per-user and transient and cannot
/// carry that.
/// </para>
/// <para>
/// Why there is no universe id: membership lives in <c>universe.UniverseResource</c>, the same as
/// data sets, so one view can serve several projects and archiving has one place to look.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "SavedView")]
public partial class SavedViewConfiguration : IGenericConfiguration
{
    /// <summary>Gets or sets the durable logical identity.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the unique view name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets the configuration section name.</summary>
    public string SectionName => "SavedViews";

    /// <summary>Gets the structural discriminator.</summary>
    public string ServiceType => "SavedView";

    /// <summary>Gets the service option type. Always null — a saved view selects no factory.</summary>
    public string? ServiceOptionType => null;

    /// <summary>Gets or sets the optional display name.</summary>
    public string? DisplayName { get; set; }

    /// <summary>Gets or sets the optional description.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets the data set this view reads.</summary>
    public Guid SubjectDataSetId { get; set; }

    /// <summary>
    /// Gets or sets the visualiser's own encoding, round-tripped opaquely. Modelling it
    /// relationally would couple this type to every chart kind that will ever exist.
    /// </summary>
    public string Encoding { get; set; } = string.Empty;

    /// <summary>Gets or sets the stored filter state, round-tripped opaquely.</summary>
    public string? Filters { get; set; }

    /// <summary>Gets or sets the chart kind.</summary>
    public string ChartType { get; set; } = string.Empty;

    /// <summary>Gets or sets the owning user.</summary>
    public Guid OwnerUserId { get; set; }

    /// <summary>Gets or sets the optional tenant scope.</summary>
    public Guid? TenantId { get; set; }

    /// <summary>Gets or sets the optional row-level visibility group.</summary>
    public Guid? VisibilityGroupId { get; set; }

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
