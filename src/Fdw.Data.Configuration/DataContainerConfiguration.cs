using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Services.Connections.Abstractions;

namespace Fdw.Services.Connections;

/// <summary>
/// Configuration class for data containers (physical schemas) at a DataPath.
/// Generates the table <c>data.DataContainer</c> as a child of <c>data.DataPath</c>.
/// </summary>
/// <remarks>
/// <para>
/// A DataContainer represents the physical schema discovered or defined at a path.
/// Storage-specific details (ObjectType, constraints, partitioning) live on typed body records
/// in <c>data.MsSqlDataContainer</c> joined by <c>RowId</c>.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration( ServiceCategory = "DataStore",
    ServiceType = "DataContainer")]
public partial class DataContainerConfiguration : IGenericConfiguration
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DataContainerConfiguration"/> class.
    /// </summary>
    public DataContainerConfiguration()
    {
    }


    /// <summary>
    /// Gets or sets the unique identifier for this container.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the name of this container for lookup and display.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets the section name for configuration binding.
    /// </summary>
    public string SectionName => "DataContainers";

    /// <summary>
    /// Gets the service type - always "DataStore" for DataContainer.
    /// </summary>
    public string ServiceType => "DataStore";

    /// <summary>
    /// Gets the service option type - null for base DataContainer.
    /// </summary>
    public string? ServiceOptionType => null;

    /// <summary>
    /// Gets or sets the DataPath ID this container is defined at.
    /// </summary>
    public Guid DataPathId { get; set; }


    /// <summary>
    /// Gets or sets the container type discriminator (e.g., "Table", "View", "JsonDocument", "CsvFile").
    /// Renamed from ContainerType to align with data.DataContainer DDL column <c>TypeId</c>.
    /// </summary>
    public string? TypeId { get; set; }

    /// <summary>
    /// Gets or sets the optional description for this container.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the serialization format discriminator this container's payload is (de)serialized
    /// with (e.g. "Json", "Xml", "Delimited", "FixedWidth"). Maps to the persisted
    /// <c>data.DataContainer.Format</c> column when present.
    /// </summary>
    /// <remarks>
    /// Why: config-driven format — the format name is consumed dynamically (it selects the
    /// <c>IFormatType</c> and the matching <c>RecordSourceTypes</c>/<c>RecordWriterTypes</c> factory),
    /// NOT through a separate FormatConfiguration typed-body provider domain. Null means inherit the
    /// owning connection transport's declared default format (e.g. Http → Json) — never a silent
    /// Tabular fallback. The row-shaping options below (<see cref="RecordSelector"/>,
    /// <see cref="FlattenNestedObjects"/>, <see cref="FlattenSeparator"/>) ride alongside on the same
    /// container config and are read directly by <c>ContainerComposition.BuildMetadata</c>.
    /// </remarks>
    [ValuesFrom(typeof(FormatTypes))]
    public string? Format { get; set; }

    /// <summary>
    /// Gets or sets the record selector that identifies the array/repeating element of row objects in
    /// the payload (JSONPath for JSON, element path for XML). Maps to
    /// <c>data.DataContainer.RecordSelector</c>.
    /// </summary>
    /// <remarks>
    /// Why: a row-shaping format option carried inline on the container config and consumed dynamically
    /// by the record-source factory via the container's <c>Metadata</c> bag — no separate typed-body
    /// FormatConfiguration provider domain.
    /// </remarks>
    public string? RecordSelector { get; set; }

    /// <summary>
    /// Gets or sets whether nested objects are flattened into dot-notation field names. Maps to
    /// <c>data.DataContainer.FlattenNestedObjects</c>.
    /// </summary>
    public bool? FlattenNestedObjects { get; set; }

    /// <summary>
    /// Gets or sets the separator used when flattening nested objects. Maps to
    /// <c>data.DataContainer.FlattenSeparator</c>.
    /// </summary>
    public string? FlattenSeparator { get; set; }

    /// <summary>
    /// Gets or sets the keys (PrimaryKey, Surrogate, Natural, Foreign, Unique) for this container.
    /// Loaded as a child collection from <c>data.DataContainerKey</c>.
    /// </summary>
#pragma warning disable MA0016 // Prefer collection abstraction — List<T> required for provider assignment
    [NotMapped]
    public List<DataContainerKeyConfiguration> Keys { get; set; } = [];

    /// <summary>
    /// Gets or sets the fields (columns) within this container.
    /// </summary>
    /// <remarks>
    /// List{T} is required for IOptions binding - configuration system needs concrete collection types.
    /// </remarks>
    public List<DataContainerFieldConfiguration> Fields { get; set; } = [];
#pragma warning restore MA0016

    // Audit columns

    /// <summary>Whether this is the current active version.</summary>
    public bool IsCurrent { get; set; } = true;

    /// <summary>Whether this record has been soft-deleted.</summary>
    public bool IsDeleted { get; set; }

    /// <summary>Original creation date from the source system.</summary>
    public DateTimeOffset? SrcCreateDate { get; set; }

    /// <summary>Timestamp when the record was created.</summary>
    public DateTimeOffset CreateDate { get; set; }

    /// <summary>Database user who created the record.</summary>
    public string CreateBy { get; set; } = string.Empty;

    /// <summary>Application user on whose behalf the record was created.</summary>
    public string CreateOnBehalfOf { get; set; } = string.Empty;

    /// <summary>Timestamp when the record was last modified.</summary>
    public DateTimeOffset ModifyDate { get; set; }

    /// <summary>Database user who last modified the record.</summary>
    public string ModifyBy { get; set; } = string.Empty;

    /// <summary>Application user on whose behalf the record was last modified.</summary>
    public string ModifyOnBehalfOf { get; set; } = string.Empty;
}
