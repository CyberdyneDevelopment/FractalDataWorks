using System;
using Fdw.Data;

namespace Fdw.Services.Universes;

/// <summary>
/// Maps to <c>universe.UniverseResource</c> — a resource attached to a universe.
/// </summary>
/// <remarks>
/// <para>
/// Why <see cref="Relationship"/> exists: a universe owns the data sets it sketched but merely
/// uses a shared connection. Without the distinction, "what does archiving a universe do" has no
/// well-defined answer, and neither does "who may edit this".
/// </para>
/// <para>
/// Why <see cref="ResourceType"/> is an unconstrained string here and in the database: the kind
/// set grows with every service domain, so it is validated against the resource-kind
/// TypeCollection at the service layer and fails loud on an unknown kind. A closed set would make
/// every new domain a schema change.
/// </para>
/// </remarks>
[GenerateMapper]
public sealed partial class UniverseResourceConfiguration
{
    /// <summary>Gets or sets the durable logical identity.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the row name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the owning universe.</summary>
    public Guid UniverseId { get; set; }

    /// <summary>Gets or sets the kind of resource attached.</summary>
    public string ResourceType { get; set; } = string.Empty;

    /// <summary>Gets or sets the attached resource's logical identity.</summary>
    public Guid ResourceId { get; set; }

    /// <summary>Gets or sets how the universe relates to the resource: Owns, Uses or Produces.</summary>
    public string Relationship { get; set; } = string.Empty;

    /// <summary>Gets or sets who attached it.</summary>
    public Guid AddedByUserId { get; set; }

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
