using System;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Services.Universes;

/// <summary>
/// Maps to <c>universe.UniverseRelationship</c> — a declared relationship between two data sets,
/// drawn as an edge on the universe map.
/// </summary>
/// <remarks>
/// This is not a join. <c>DataSetJoin</c> describes how one compound data set assembles its own
/// sources; a relationship is a claim about the model that holds whether or not anything joins on
/// it. Both field references are nullable because the map shows the unresolved state — a
/// relationship that is known to exist before anyone has said which columns carry it.
/// </remarks>
[GenerateMapper]
public sealed partial class UniverseRelationshipConfiguration : IGenericConfiguration
{
    /// <summary>Gets or sets the durable logical identity.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the row name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets the configuration section name.</summary>
    public string SectionName => "UniverseRelationships";

    /// <summary>Gets the structural discriminator.</summary>
    public string ServiceType => "UniverseRelationship";

    /// <summary>Gets the service option type. Always null — this row selects no factory.</summary>
    public string? ServiceOptionType => null;

    /// <summary>Gets or sets the owning universe.</summary>
    public Guid UniverseId { get; set; }

    /// <summary>Gets or sets the data set on the left of the relationship.</summary>
    public Guid LeftDataSetId { get; set; }

    /// <summary>Gets or sets the left field, when the join key has been named.</summary>
    public Guid? LeftFieldId { get; set; }

    /// <summary>Gets or sets the data set on the right of the relationship.</summary>
    public Guid RightDataSetId { get; set; }

    /// <summary>Gets or sets the right field, when the join key has been named.</summary>
    public Guid? RightFieldId { get; set; }

    /// <summary>Gets or sets the cardinality: OneToOne, OneToMany or ManyToMany.</summary>
    public string Cardinality { get; set; } = string.Empty;

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
