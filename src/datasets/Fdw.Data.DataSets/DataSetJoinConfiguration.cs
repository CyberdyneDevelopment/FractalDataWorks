using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;

namespace Fdw.Data.DataSets.Abstractions;

/// <summary>
/// Defines one join between two sources of a DataSet: which field on the left source matches which
/// field on the right source, and how the rows combine. Maps to the <c>data.DataSetJoin</c> table in
/// ConfigurationDb.
/// </summary>
/// <remarks>
/// <para>
/// A composed child of a Compound or Federated data set (<see cref="IJoinedDataSetConfiguration"/>), saved and loaded with it
/// through <c>DataSetJoinConfigurationCommand</c> the same way <see cref="DataSetAggregateConfiguration"/>
/// is. The row's link to its dataset (<c>CompoundDataSetConfigurationRowId</c> or <c>FederatedDataSetConfigurationRowId</c>,
/// whichever kind owns it) is filled by the provider from the foreign key its owner's container lists, so it is not a
/// property here.
/// </para>
/// <para>
/// Joins run in ascending <see cref="Ordinal"/> order, each building on the result of the ones before
/// it. A Compound dataset pushes them down as JOIN clauses; a Federated dataset performs them in
/// memory. Source and field names are logical: <see cref="LeftSourceName"/> and
/// <see cref="RightSourceName"/> name a <c>DataSetSourceConfiguration.SourceName</c> of the same dataset.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "DataSetJoin")]
public sealed partial class DataSetJoinConfiguration : IGenericConfiguration
{
    /// <summary>Gets or sets the logical identifier for this join definition.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the name (computed; not a persisted column).</summary>
    public string Name { get; set; } = string.Empty;

    // ============================================================================
    // Join definition
    // ============================================================================

    /// <summary>
    /// Gets or sets the name of the left source — a <c>SourceName</c> of this dataset's sources, or of
    /// a source already joined by an earlier join.
    /// </summary>
    public string LeftSourceName { get; set; } = string.Empty;

    /// <summary>Gets or sets the logical field name on the left source to join on.</summary>
    public string LeftFieldName { get; set; } = string.Empty;

    /// <summary>Gets or sets the name of the right source — the source being joined to the left one.</summary>
    public string RightSourceName { get; set; } = string.Empty;

    /// <summary>Gets or sets the logical field name on the right source to join on.</summary>
    public string RightFieldName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the join type: <c>Inner</c>, <c>Left</c>, <c>Right</c>, <c>Full</c> or <c>Cross</c>.
    /// </summary>
    /// <remarks>Why no default here: the authored value always flows through from the create/update request.</remarks>
    public string JoinType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the execution order of this join within its DataSet. Lower values run first.
    /// Unique per DataSet (enforced per owner by UX_DataSetJoin_DataSetConfigurationRowId_Ordinal_Current_Compound and _Federated).
    /// </summary>
    public int Ordinal { get; set; }

    /// <summary>Gets or sets an optional description of the join's purpose.</summary>
    public string? Description { get; set; }

    // ============================================================================
    // Version-on-write / soft-delete
    // ============================================================================

    /// <summary>Gets or sets whether this is the current active version of the record.</summary>
    public bool IsCurrent { get; set; } = true;

    /// <summary>Gets or sets whether this record has been soft-deleted.</summary>
    public bool IsDeleted { get; set; }

    // ============================================================================
    // Audit
    // ============================================================================

    /// <summary>Gets or sets the original creation date from the source system (if migrated).</summary>
    public DateTimeOffset? SrcCreateDate { get; set; }

    /// <summary>Gets the timestamp when the record was created.</summary>
    public DateTimeOffset CreateDate { get; set; }

    /// <summary>Gets the database user who created the record.</summary>
    public string CreateBy { get; set; } = string.Empty;

    /// <summary>Gets the application user on whose behalf the record was created.</summary>
    public string CreateOnBehalfOf { get; set; } = string.Empty;

    /// <summary>Gets or sets the timestamp when the record was last modified.</summary>
    public DateTimeOffset ModifyDate { get; set; }

    /// <summary>Gets or sets the database user who last modified the record.</summary>
    public string ModifyBy { get; set; } = string.Empty;

    /// <summary>Gets or sets the application user on whose behalf the record was last modified.</summary>
    public string ModifyOnBehalfOf { get; set; } = string.Empty;
}
