using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Configuration;
using Fdw.Data.DataSets.Abstractions;

namespace Fdw.Data.DataSets.Abstractions;

/// <summary>
/// Configuration for a DataSet source — the physical binding from a logical DataSet
/// to a connection, DataStore, and container.
/// </summary>
/// <remarks>
/// Physical binding record: one row per source in a DataSet. The strategy discriminator
/// (Simple/Compound/Federated) lives on the parent <c>DataSetConfiguration.ServiceOptionType</c>,
/// never on the source row.
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration( ServiceCategory = "DataSet",
    ServiceType = "DataSetSource")]
public sealed partial class DataSetSourceConfiguration : IGenericConfiguration
{
    /// <summary>Gets or sets the unique identifier for this source configuration.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the name of this source configuration.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets the section name for configuration binding.</summary>
    public string SectionName => $"DataSetSources:{Id}";

    /// <summary>Gets or sets the service type domain ("DataSet").</summary>
    public string ServiceType { get; set; } = "DataSet";

    /// <summary>Gets or sets the service option type discriminator ("DataSetSource").</summary>
    public string? ServiceOptionType { get; set; } = "DataSetSource";

    /// <summary>Gets or sets whether this is the current active version of the record.</summary>
    public bool IsCurrent { get; set; } = true;

    /// <summary>Gets or sets whether this record has been soft-deleted.</summary>
    public bool IsDeleted { get; set; }

    // ============================================================================
    // Identity
    // ============================================================================

    /// <summary>Gets or sets the parent DataSet identifier (FK to data.DataSet.Id).</summary>
    public Guid DataSetId { get; set; }

    // ============================================================================
    // Source binding
    // ============================================================================

    /// <summary>
    /// Gets or sets the source name (e.g., "Primary", "Fallback").
    /// Must be unique within a DataSet.
    /// </summary>
    public string SourceName { get; set; } = string.Empty;

    /// <summary>Gets or sets the DataStore name that owns this source's container.</summary>
    public string DataStoreName { get; set; } = string.Empty;

    /// <summary>Gets or sets the connection name to use for this source.</summary>
    public string ConnectionName { get; set; } = string.Empty;

    /// <summary>Gets or sets the connection type name (e.g., "MsSql", "Http", "File").</summary>
    public string ConnectionType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the DataContainer identifier for this source.
    /// Resolves via the DataStore tree: DataStore → Path → Container.
    /// </summary>
    public Guid? ContainerId { get; set; }

    /// <summary>
    /// Gets or sets the path name within the DataStore (e.g., the schema/namespace).
    /// Used together with <see cref="ContainerName"/> to address the physical container.
    /// </summary>
    /// <remarks>
    /// Named PathValue and not Path: a member called Path shadows <see cref="System.IO.Path"/> inside
    /// the declaring type, so <c>Path.Combine(...)</c> there resolves to this value and fails to
    /// compile in a way that reads as nonsense.
    /// </remarks>
    public string PathValue { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the container name within the path (e.g., the table/view name).
    /// Used together with <see cref="PathValue"/> to address the physical container.
    /// </summary>
    public string ContainerName { get; set; } = string.Empty;

    /// <summary>Gets or sets the source DataSet identifier when SourceKind='DataSet' (FK to data.DataSet.Id).</summary>
    /// <remarks>Non-null only when this source binds to another DataSet (compound/federated join).</remarks>
    public Guid? SourceDataSetId { get; set; }

    /// <summary>Gets or sets the source DataSet name for display when SourceKind='DataSet'.</summary>
    /// <remarks>Denormalized from SourceDataSetId for UI convenience; used when resolving compound/federated sources.</remarks>
    public string? SourceDataSetName { get; set; }

    /// <summary>
    /// Gets or sets the source kind discriminator ("DataStore" or "DataSet" — a registered
    /// <c>SourceKinds</c> member) that determines how this source is opened at execution time.
    /// </summary>
    /// <remarks>
    /// Why: no default here — the DDL column default ('DataStore') only applies when the app omits the
    /// column from an INSERT; the authored value always flows through from the create/update request.
    /// </remarks>
    public string SourceKind { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether this is the primary source — for a Compound dataset, the single source
    /// whose container is the pushed-down query's FROM clause.
    /// </summary>
    public bool IsPrimary { get; set; }

    /// <summary>Gets or sets the priority of this source (lower = higher priority).</summary>
    public int Priority { get; set; } = 100;

    /// <summary>Gets or sets whether this source supports predicate pushdown optimization.</summary>
    public bool SupportsPredicatePushdown { get; set; } = true;

    // ============================================================================
    // HTTP-specific
    // ============================================================================

    /// <summary>Gets or sets the HTTP endpoint path for HTTP sources.</summary>
    public string? HttpEndpoint { get; set; }

    /// <summary>Gets or sets the HTTP method for HTTP sources.</summary>
    public string? HttpMethod { get; set; }

    // ============================================================================
    // File-specific
    // ============================================================================

    /// <summary>Gets or sets the file path for file-based sources.</summary>
    public string? FilePath { get; set; }

    /// <summary>Gets or sets the file format for file-based sources.</summary>
    public string? FileFormat { get; set; }

    // ============================================================================
    // Row mapping
    // ============================================================================

    /// <summary>Gets or sets the row mapper type name for this source.</summary>
    public string? MapperTypeName { get; set; }

    /// <summary>
    /// Gets or sets the record selector expression that identifies repeating record
    /// elements in the payload (e.g., XPath "//Report/Data/Row", JSONPath "$.data[*]").
    /// </summary>
    /// <remarks>
    /// Why: config-driven row shaping carried inline on the source config and consumed dynamically by
    /// the record source — no separate FormatConfiguration typed-body provider domain. Maps to the
    /// persisted <c>data.DataSetSource.RecordSelector</c> column.
    /// </remarks>
    public string? RecordSelector { get; set; }

    // ============================================================================
    // Field mapping FK references
    // ============================================================================

    /// <summary>Gets or sets the IDs of field mapping configurations for this source.</summary>
    public IList<Guid> FieldMappingIds { get; set; } = new List<Guid>();

    /// <summary>
    /// Gets or sets the resolved field mappings for this source (logical→physical).
    /// </summary>
    /// <remarks>
    /// Why: [NotMapped] — composed at runtime from DataSetFieldMapping rows by
    /// DataSetConfigurationProvider; never a column on data.DataSetSource.
    /// </remarks>
    [NotMapped]
    public IReadOnlyDictionary<string, string> FieldMappings { get; set; } = new Dictionary<string, string>(StringComparer.Ordinal);

    /// <summary>
    /// Gets or sets the field mappings this source owns.
    /// </summary>
    /// <remarks>
    /// Why this exists beside <see cref="FieldMappings"/>: that one is the logical→physical lookup
    /// the query path reads and is computed, not stored. This is the stored collection, and being a
    /// List of a mapped configuration is what makes it a child of the aggregate — so saving the
    /// data set writes its mappings and the cascade fills in the row key that ties them to this
    /// source. Written any other way the insert has no parent key to supply and the column refuses
    /// it, which is what "field mappings could not be saved" meant.
    /// </remarks>
#pragma warning disable MA0016 // Prefer collection abstraction - required for IOptions binding
    public List<DataSetFieldMappingConfiguration> Mappings { get; set; } = [];
#pragma warning restore MA0016

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
