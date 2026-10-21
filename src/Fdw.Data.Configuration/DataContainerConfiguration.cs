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
    /// Gets or sets the container type discriminator (e.g., "Table", "View", "JsonDocument", "CsvFile").
    /// Renamed from ContainerType to align with data.DataContainer DDL column <c>TypeId</c>.
    /// </summary>
    public string? TypeId { get; set; }

    /// <summary>
    /// Gets or sets the optional description for this container.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets how sensitive this data is: the <c>ClassificationId</c> of a <c>DataClassifications</c>
    /// level. <c>NotYetClassified</c> until someone picks one; never empty. Maps to <c>ClassificationId</c> (FDW-938).
    /// </summary>
    public int ClassificationId { get; set; } = DataClassifications.NotYetClassified.Id;

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
    /// Gets or sets whether the first row of the file is a header row, for a Delimited or Xlsx container. Maps to
    /// <c>data.DataContainer.HasHeader</c>. Null means not stated: the reader then reads the first row as data.
    /// </summary>
    public bool? HasHeader { get; set; }

    /// <summary>
    /// Gets or sets the character or string that separates the fields of a row, for a Delimited container. Maps to
    /// <c>data.DataContainer.Separator</c>. This is the file's field separator, not <see cref="FlattenSeparator"/>.
    /// </summary>
    public string? Separator { get; set; }

    /// <summary>
    /// Gets or sets the name of the worksheet to read, for an Xlsx container. Maps to <c>data.DataContainer.SheetName</c>.
    /// Null means not stated: a workbook with a single worksheet is read, one with several is refused.
    /// </summary>
    public string? SheetName { get; set; }

    /// <summary>
    /// Gets or sets the path, after the connection's address, that the rows are sent to when this container is the destination
    /// of an HTTP connection, for example <c>/api/v1/ingest</c>. Maps to <c>data.DataContainer.WriteEndpoint</c>. Null means
    /// not stated: a destination that needs it fails naming it.
    /// </summary>
    public string? WriteEndpoint { get; set; }

    /// <summary>
    /// Gets or sets the HTTP method the rows are sent with, <c>POST</c> or <c>PUT</c>. Maps to
    /// <c>data.DataContainer.WriteMethod</c>. Null means not stated: a destination that needs it fails naming it.
    /// </summary>
    public string? WriteMethod { get; set; }

    /// <summary>
    /// Gets or sets the content type the rows are sent as, for example <c>application/json</c>. Maps to
    /// <c>data.DataContainer.WriteContentType</c>. Null means not stated: a destination that needs it fails naming it.
    /// </summary>
    public string? WriteContentType { get; set; }

    /// <summary>
    /// Gets or sets the fields (columns) within this container.
    /// </summary>
    /// <remarks>
    /// List{T} is required for IOptions binding - configuration system needs concrete collection types.
    /// Declared before <see cref="Keys"/>: the generic cascade save walks child collections in
    /// declaration order, so a container's fields are written before the keys that cite them. A key
    /// field cites its field by the field's Id (DataContainerFieldId), which the field keeps across
    /// saves.
    /// </remarks>
#pragma warning disable MA0016 // Prefer collection abstraction — List<T> required for provider assignment
    public List<DataContainerFieldConfiguration> Fields { get; set; } = [];

    /// <summary>
    /// Gets or sets the keys (PrimaryKey, Surrogate, Natural, Foreign, Unique) for this container.
    /// Loaded as a child collection from <c>data.DataContainerKey</c>.
    /// </summary>
    [NotMapped]
    public List<DataContainerKeyConfiguration> Keys { get; set; } = [];
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
