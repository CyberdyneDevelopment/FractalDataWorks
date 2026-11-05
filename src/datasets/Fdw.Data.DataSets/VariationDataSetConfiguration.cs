using Fdw.Configuration;
using Fdw.Data.Abstractions;
using Fdw.Data;
using Fdw.Schema.Keys;
using Fdw.Schema.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Fdw.Data.DataSets.Abstractions;

/// <summary>The Variation implementation of DataSet: a data set that is a view of another data set (FDW-3638).</summary>
/// <remarks>
/// Maps to <c>data.VariationDataSetConfiguration</c>, keyed to its domain row in <c>data.DataSet</c>. A variation has no
/// fields, sources or key fields of its own: the lists below are empty by design, because it reads the data set it varies
/// and applies the filters of its chain. Who owns it tells a Scoped variation (a workspace) from a data view (a user).
/// </remarks>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[GenerateMapper]
public sealed partial class VariationDataSetConfiguration : IDataSetVariationImplementationConfiguration
{
    /// <summary>The implementation name a variation's domain row carries.</summary>
    public const string ImplementationName = "Variation";

    /// <inheritdoc/>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the domain.</summary>
    public string Domain { get; set; } = string.Empty;

    /// <inheritdoc/>
    /// <remarks>Set by the provider from the domain row; never persisted.</remarks>
    public string Implementation { get; set; } = string.Empty;

    /// <inheritdoc/>
    public string VariationName { get; set; } = string.Empty;

    /// <summary>Gets or sets the display name.</summary>
    public string? DisplayName { get; set; }

    /// <summary>Gets or sets the abbreviation.</summary>
    public string? Abbreviation { get; set; }

    /// <summary>Gets or sets the is sortable.</summary>
    public bool IsSortable { get; set; }

    /// <summary>Gets or sets the is filterable.</summary>
    public bool IsFilterable { get; set; }

    /// <summary>Gets or sets the source data set name.</summary>
    public string? SourceDataSetName { get; set; }

    /// <summary>Gets or sets the description.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets how sensitive this variation's data is: the <c>ClassificationId</c> of a <c>DataClassifications</c> level, its own
    /// and not its base data set's (FDW-1027). <c>NotYetClassified</c> until someone picks one; never empty.
    /// </summary>
    public int ClassificationId { get; set; } = DataClassifications.NotYetClassified.Id;

    /// <summary>Gets or sets the version. A variation stores none (its table has no column), so nothing is assumed for it (FDW-3956).</summary>
    public string Version { get; set; } = string.Empty;

    /// <inheritdoc />
    public Guid? CategoryId { get; set; }

    /// <summary>Gets or sets the record type name.</summary>
    public string RecordTypeName { get; set; } = string.Empty;

    /// <summary>Gets or sets the fields; a variation has none of its own.</summary>
    [NotStored]
    public IList<DataSetFieldConfiguration> Fields { get; set; } = new List<DataSetFieldConfiguration>();

    /// <summary>Gets or sets the sources; a variation has none, it varies <see cref="BaseDataSetId"/>.</summary>
    [NotStored]
    public IList<DataSetSourceConfiguration> Sources { get; set; } = new List<DataSetSourceConfiguration>();

    /// <summary>Gets or sets the caching.</summary>
    public CachingConfiguration? Caching { get; set; }

    /// <summary>Gets or sets the filter conditions every data set carries; a variation's filter is <see cref="StoredFilters"/>.</summary>
    [NotStored]
    public IList<DataSetFilterConditionConfiguration> Filters { get; set; } = new List<DataSetFilterConditionConfiguration>();

    /// <summary>Gets or sets the key fields; a variation has none of its own.</summary>
    [NotStored]
    public IList<DataSetKeyFieldConfiguration> KeyFields { get; set; } = new List<DataSetKeyFieldConfiguration>();

    /// <summary>Gets the durable ids of this data set's sources.</summary>
    public IReadOnlyList<Guid> SourceIds => Sources.Select(s => s.Id).ToList();

    /// <inheritdoc/>
    public Guid? OwnerWorkspaceId { get; set; }

    /// <inheritdoc/>
    public Guid? OwnerUserId { get; set; }

    /// <inheritdoc/>
    public Guid BaseDataSetId { get; set; }

    /// <inheritdoc/>
    public Guid? ParentVariationId { get; set; }

    /// <inheritdoc/>
    public string? StoredFilters { get; set; }

    /// <inheritdoc/>
    public string? Encoding { get; set; }

    /// <inheritdoc/>
    public string? ChartType { get; set; }

    /// <inheritdoc/>
    public DateTimeOffset CreatedAt { get; set; }

    /// <inheritdoc/>
    public Guid? TenantId { get; set; }

    /// <summary>Gets the kind of variation this is, from who owns it; empty until an owner is set.</summary>
    public string VariationKind => OwnerWorkspaceId is not null ? VariationKinds.Scoped : OwnerUserId is not null ? VariationKinds.DataView : string.Empty;

    /// <summary>Gets or sets the optional row-level visibility group.</summary>
    public Guid? VisibilityGroupId { get; set; }

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
