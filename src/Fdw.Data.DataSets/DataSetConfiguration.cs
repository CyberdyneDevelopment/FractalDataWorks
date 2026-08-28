using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Schema.Keys;
using Fdw.Schema.Properties;

namespace Fdw.Data.DataSets.Abstractions;

/// <summary>
/// Configuration class for all dataset types — Standard, Compound, and Federated.
/// Maps to the single <c>data.DataSet</c> table in ConfigurationDb.
/// </summary>
/// <remarks>
/// <para>
/// The DataSet hierarchy has been flattened into a single table.
/// <c>DataSetType</c> on DataSetSource identifies the execution variant (Standard, MultiSource, Distributed).
/// Properties that belong to only one variant are nullable on all rows of other variants.
/// </para>
/// <para>
/// DataSetConfiguration is the root configuration for the DataSet hierarchy:
/// <list type="bullet">
/// <item><description>DataSetConfiguration (parent) - Logical dataset definition</description></item>
/// <item><description>DataSetSourceConfiguration (child entity) - Physical data sources (1:many via DataSetId FK)</description></item>
/// <item><description>DataSetFieldMappingConfiguration (grandchild entity) - Field mappings per source (1:many via DataSetSourceId FK)</description></item>
/// </list>
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration( ServiceCategory = "DataSet")]
public partial class DataSetConfiguration : IGenericConfiguration
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DataSetConfiguration"/> class.
    /// </summary>
    /// <remarks>
    /// Why: only the structural discriminator (<c>ServiceType</c>) and section name are set here —
    /// fixed constants for every dataset. <c>ServiceOptionType</c> is the AUTHORED strategy kind
    /// (Simple/Compound/Federated) and is NOT defaulted: a missing value must fail loud at dispatch,
    /// not be silently substituted (NO FALLBACKS).
    /// </remarks>
    public DataSetConfiguration()
    {
        ServiceType = "DataSet";
        SectionName = "DataSets";
    }


    /// <summary>
    /// Gets or sets the unique identifier for this dataset.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the name of the dataset.
    /// </summary>
    /// <value>The unique, code-stable identifier for this dataset.</value>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the human-facing display name for this dataset.
    /// Falls back to <see cref="Name"/> when null or empty.
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// Gets or sets an optional short abbreviation for this dataset (≤20 chars).
    /// Used in compact UI contexts (breadcrumbs, tab labels, chips).
    /// </summary>
    public string? Abbreviation { get; set; }

    /// <summary>
    /// Gets or sets the section name for configuration binding.
    /// </summary>
    public string SectionName { get; set; }

    /// <summary>
    /// Gets or sets the service type (domain) - always "DataSet" for this configuration.
    /// </summary>
    public string ServiceType { get; set; }

    /// <summary>
    /// Gets or sets the service option type (not used for DataSets).
    /// </summary>
    [ValuesFrom(typeof(DataSetTypes))]
    public string? ServiceOptionType { get; set; }

    // ── Type-specific properties (flattened from former child tables) ────────────

    /// <summary>
    /// Gets or sets whether this dataset supports client-side sorting in data preview.
    /// Applies to Standard, Compound, and Federated dataset types.
    /// </summary>
    public bool IsSortable { get; set; }

    /// <summary>
    /// Gets or sets whether this dataset supports client-side filtering in data preview.
    /// Applies to Standard, Compound, and Federated dataset types.
    /// </summary>
    public bool IsFilterable { get; set; }

    /// <summary>
    /// Gets or sets the SQL or expression that transforms the source data for a Compound dataset.
    /// Null for Standard and Federated datasets.
    /// </summary>
    public string? TransformExpression { get; set; }

    /// <summary>
    /// Gets or sets the name of the source dataset this Compound dataset is derived from.
    /// Null for Standard and Federated datasets.
    /// </summary>
    public string? SourceDataSetName { get; set; }

    /// <summary>
    /// Gets or sets the federation strategy used to combine sources for a Federated dataset
    /// (e.g., "Sequential", "Parallel", "Optimized").
    /// Null for Standard and Compound datasets.
    /// </summary>
    [ValuesFrom("FederationStrategies")]
    public string? FederationStrategy { get; set; }

    /// <summary>
    /// Gets or sets the description of the dataset.
    /// </summary>
    /// <value>A detailed description explaining the purpose and content of this dataset.</value>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the version of this dataset schema.
    /// </summary>
    /// <value>The schema version string for compatibility and migration purposes.</value>
    public string Version { get; set; } = "1.0";

    /// <summary>
    /// Gets or sets the category for grouping related datasets.
    /// </summary>
    /// <value>The category name for organizational purposes.</value>
    public string Category { get; set; } = "Dataset";

    /// <summary>
    /// Gets or sets the optional category identifier for linking to cfg.Category.
    /// </summary>
    /// <value>The foreign key to the Category table, or null if uncategorized.</value>
    public Guid? CategoryId { get; set; }

    /// <summary>
    /// Gets or sets the .NET type name of the record/entity that this dataset represents.
    /// </summary>
    /// <value>The fully qualified type name of the data record or entity class.</value>
    public string RecordTypeName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the field definitions that define the schema of this dataset.
    /// </summary>
    /// <value>A collection of field configuration objects describing the structure of records.</value>
    public IList<DataFieldConfiguration> Fields { get; set; } = new List<DataFieldConfiguration>();

    /// <summary>
    /// Gets the IDs of the source configurations for this DataSet, derived from <see cref="Sources"/>.
    /// </summary>
    /// <remarks>
    /// Why: computed from the composed Sources child collection — no separate SourceIds storage. The
    /// keystone read cascade populates <see cref="Sources"/>; this projects their Ids for the external
    /// source resolver. The `?? []` is the single sanctioned empty-collection fallback (CLAUDE.md).
    /// </remarks>
    public IReadOnlyList<Guid> SourceIds => Sources?.Select(s => s.Id).ToList() ?? [];

    /// <summary>
    /// Gets or sets the fully-loaded source configurations.
    /// </summary>
    /// <remarks>
    /// Populated by <c>DataSetConfigurationProvider.Get(name)</c> / <c>Get(id)</c>. List/summary
    /// queries leave this empty to avoid N×3 child queries on grid renders. Callers that need
    /// container resolution (e.g. /data-preview/statset) read this collection.
    /// </remarks>
    public IList<DataSetSourceConfiguration> Sources { get; set; } = new List<DataSetSourceConfiguration>();

    /// <summary>
    /// Gets or sets join definitions for combining data from multiple sources.
    /// </summary>
    /// <value>
    /// A collection of join configurations that define how to merge data from different sources.
    /// Joins are performed in the order specified in this list, with each join building on
    /// the results of previous joins.
    /// </value>
    /// <remarks>
    /// For datasets with multiple sources, joins define how to combine the data.
    /// If no joins are specified for a multi-source dataset, a Cartesian product is used (not recommended).
    /// For single-source datasets, this property should be empty.
    /// </remarks>
    public IList<JoinConfiguration> Joins { get; set; } = new List<JoinConfiguration>();

    /// <summary>
    /// Gets or sets caching configuration for this dataset.
    /// </summary>
    /// <value>Configuration settings for data caching behavior.</value>
    public CachingConfiguration? Caching { get; set; }

    /// <summary>
    /// Gets or sets the stored filter conditions applied automatically when querying this dataset.
    /// </summary>
    /// <value>A collection of filter conditions that narrow the result set for every query against this dataset.</value>
    public IList<DataSetFilterConditionConfiguration> Filters { get; set; } = new List<DataSetFilterConditionConfiguration>();

    /// <summary>
    /// Gets or sets the aggregate measure definitions for this dataset, describing group-by keys
    /// and aggregate functions (SUM, AVG, COUNT, MIN, MAX) applied to input fields to produce
    /// named output columns.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Stores group-by and measure definitions at the dataset level, enabling reuse of aggregation
    /// logic across queries without repeating it in pipeline transformation configurations.
    /// For example, a "sales by state" dataset can carry two definitions:
    /// </para>
    /// <list type="bullet">
    /// <item><description>TotalSales — SUM of Amount, grouped by State</description></item>
    /// <item><description>TransactionCount — COUNT of Id, grouped by State</description></item>
    /// </list>
    /// <para>
    /// Populated by the provider's full <c>Get(name)</c> / <c>Get(id)</c> overloads via the
    /// ComposeChildren cascade from <c>data.DataSetAggregate</c> (IsCurrent=1, IsDeleted=0,
    /// joined by DataSetRowId). List/summary queries that skip Sources and Joins also skip
    /// Aggregates to avoid N×4 child queries on grid renders.
    /// </para>
    /// </remarks>
    public IList<DataSetAggregateDefinition> Aggregates { get; set; } = new List<DataSetAggregateDefinition>();

    /// <summary>
    /// Gets or sets whether this is the current active version of the record.
    /// </summary>
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

    /// <summary>
    /// Gets or sets the key fields for this dataset (Surrogate, Natural, Foreign).
    /// Loaded from <c>data.DataSetKeyField</c>. Callers filter by <see cref="DataSetKeyFieldConfiguration.KeyType"/>.
    /// </summary>
#pragma warning disable MA0016 // Prefer collection abstraction — List<T> required for provider assignment
    public List<DataSetKeyFieldConfiguration> KeyFields { get; set; } = [];
#pragma warning restore MA0016

}
