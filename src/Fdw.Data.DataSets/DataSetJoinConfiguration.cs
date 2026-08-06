using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Data;
using Fdw.Data.DataSets.Abstractions;

namespace Fdw.Data.DataSets;

/// <summary>
/// Configuration for a join operation between two DataSet sources.
/// Used in federated datasets to define how multiple sources are combined.
/// </summary>
/// <remarks>
/// <para>
/// Defines a join between two sources within a DataSet. Joins are executed
/// in order based on the Ordinal property, with each join building on the
/// results of previous joins.
/// </para>
/// <para>
/// This configuration supports row-level versioning via the Id/RowId pattern:
/// <list type="bullet">
/// <item><description>Id - Logical identifier that persists across versions</description></item>
/// <item><description>RowId - Version-specific row identifier (primary key in database)</description></item>
/// <item><description>IsCurrent - Indicates the active version</description></item>
/// <item><description>IsDeleted - Soft delete flag</description></item>
/// </list>
/// </para>
/// <para>
/// Database table: data.DataSetJoin
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
public sealed partial class DataSetJoinConfiguration
{

    /// <summary>
    /// Gets or sets the logical identifier for this join definition.
    /// </summary>
    /// <remarks>
    /// This identifier persists across versions. When a join configuration
    /// is updated, a new RowId is created but Id remains the same.
    /// </remarks>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the parent DataSet identifier.
    /// </summary>
    /// <remarks>
    /// Foreign key to the parent DataSet's logical Id.
    /// References data.DataSet.Id (where IsCurrent=1).
    /// </remarks>
    public Guid DataSetId { get; set; }


    /// <summary>
    /// Gets or sets the name of the left source in the join.
    /// </summary>
    /// <remarks>
    /// References DataSetSource.SourceName within the same DataSet.
    /// For the first join, this is typically the primary source.
    /// For subsequent joins, this should reference a previously joined source.
    /// </remarks>
    public string LeftSourceName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the logical field name in the left source to join on.
    /// </summary>
    /// <remarks>
    /// Must correspond to a field name in the left source's schema.
    /// </remarks>
    public string LeftFieldName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the right source in the join.
    /// </summary>
    /// <remarks>
    /// References DataSetSource.SourceName within the same DataSet.
    /// This is the source being joined to the left source.
    /// </remarks>
    public string RightSourceName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the logical field name in the right source to join on.
    /// </summary>
    /// <remarks>
    /// Must correspond to a field name in the right source's schema.
    /// </remarks>
    public string RightFieldName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the type of join operation.
    /// </summary>
    /// <value>
    /// One of: "Inner", "Left", "Right", "Full", or "Cross".
    /// Default is "Inner".
    /// </value>
    /// <remarks>
    /// Constrained by CK_DataSetJoin_JoinType check constraint in the database.
    /// </remarks>
    public string JoinType { get; set; } = "Inner";

    /// <summary>
    /// Gets or sets the execution order for this join.
    /// </summary>
    /// <value>
    /// Zero-based ordinal. Joins are performed sequentially in ascending order,
    /// with each join building on the results of previous joins.
    /// </value>
    /// <remarks>
    /// Must be unique within a DataSet (enforced by unique index on DataSetId + Ordinal
    /// where IsCurrent=1).
    /// </remarks>
    public int Ordinal { get; set; }

    /// <summary>
    /// Gets or sets an optional description of the join purpose and semantics.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets whether this is the current active version of the record.
    /// </summary>
    /// <remarks>
    /// Only one version of a logical join (by Id) can have IsCurrent=1 at a time.
    /// Enforced by unique index UX_DataSetJoin_Id_Current.
    /// </remarks>
    public bool IsCurrent { get; set; } = true;

    /// <summary>
    /// Gets or sets whether this record has been soft-deleted.
    /// </summary>
    public bool IsDeleted { get; set; }

    /// <summary>
    /// Gets or sets the original creation date from the source system (if migrated).
    /// </summary>
    public DateTimeOffset? SrcCreateDate { get; set; }

    /// <summary>
    /// Gets the timestamp when the record was created in this system.
    /// </summary>
    public DateTimeOffset CreateDate { get; set; }

    /// <summary>
    /// Gets the database user who created the record.
    /// </summary>
    public string CreateBy { get; set; } = string.Empty;

    /// <summary>
    /// Gets the application user on whose behalf the record was created.
    /// </summary>
    public string CreateOnBehalfOf { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timestamp when the record was last modified.
    /// </summary>
    public DateTimeOffset ModifyDate { get; set; }

    /// <summary>
    /// Gets or sets the database user who last modified the record.
    /// </summary>
    public string ModifyBy { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the application user on whose behalf the record was last modified.
    /// </summary>
    public string ModifyOnBehalfOf { get; set; } = string.Empty;
}
