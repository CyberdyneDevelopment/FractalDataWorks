using Fdw.Configuration;
using Fdw.Data;
using Fdw.Schema.Keys;
using Fdw.Schema.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Fdw.Data.DataSets.Abstractions;

/// <summary>The DataSet implementation's own configuration.</summary>
/// <remarks>
/// Every property here was on the domain record until the split. None of it is something the
/// DataSet domain could hold an opinion about for every implementation of itself.
/// </remarks>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[GenerateMapper]
public sealed partial class DataSetImplementationConfiguration : IDataSetImplementationConfiguration
{
    /// <inheritdoc/>
    /// <summary>Gets or sets the id.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the domain.</summary>
    public string Domain { get; set; } = string.Empty;

    /// <inheritdoc/>
    /// <remarks>Set by the provider from the domain row; never persisted.</remarks>
    public string Implementation { get; set; } = string.Empty;

    /// <summary>Gets or sets the data set id.</summary>
    public Guid DataSetId { get; set; }

    /// <summary>Gets or sets the data set row id.</summary>
    public int DataSetRowId { get; set; }

    /// <summary>Gets or sets the display name.</summary>
    public string? DisplayName { get; set; }

    /// <summary>Gets or sets the abbreviation.</summary>
    public string? Abbreviation { get; set; }

    /// <summary>Gets or sets the is sortable.</summary>
    public bool IsSortable { get; set; }

    /// <summary>Gets or sets the is filterable.</summary>
    public bool IsFilterable { get; set; }

    /// <summary>Gets or sets the transform expression.</summary>
    public string? TransformExpression { get; set; }

    /// <summary>Gets or sets the source data set name.</summary>
    public string? SourceDataSetName { get; set; }

    /// <summary>Gets or sets the federation strategy.</summary>
    public string? FederationStrategy { get; set; }

    /// <summary>Gets or sets the description.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Gets or sets the version.</summary>
    public string Version { get; set; } = "1.0";

    /// <summary>Gets or sets the category.</summary>
    public string Category { get; set; } = "Dataset";

    /// <summary>Gets or sets the category id.</summary>
    public Guid? CategoryId { get; set; }

    /// <summary>Gets or sets the record type name.</summary>
    public string RecordTypeName { get; set; } = string.Empty;

    /// <summary>Gets or sets the fields.</summary>
    public IList<DataSetFieldConfiguration> Fields { get; set; } = new List<DataSetFieldConfiguration>();

    /// <summary>Gets or sets the sources.</summary>
    public IList<DataSetSourceConfiguration> Sources { get; set; } = new List<DataSetSourceConfiguration>();

    /// <summary>Gets or sets the joins.</summary>
    public IList<JoinConfiguration> Joins { get; set; } = new List<JoinConfiguration>();

    /// <summary>Gets or sets the caching.</summary>
    public CachingConfiguration? Caching { get; set; }

    /// <summary>Gets or sets the filters.</summary>
    public IList<DataSetFilterConditionConfiguration> Filters { get; set; } = new List<DataSetFilterConditionConfiguration>();

    /// <summary>Gets or sets the aggregates.</summary>
    public IList<DataSetAggregateConfiguration> Aggregates { get; set; } = new List<DataSetAggregateConfiguration>();

    /// <summary>Gets or sets the key fields.</summary>
    public IList<DataSetKeyFieldConfiguration> KeyFields { get; set; } = new List<DataSetKeyFieldConfiguration>();

    /// <summary>Gets the durable ids of this dataset's sources.</summary>
    public IReadOnlyList<Guid> SourceIds => Sources?.Select(s => s.Id).ToList() ?? [];

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
