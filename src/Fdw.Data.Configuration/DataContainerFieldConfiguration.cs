using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Services.Connections.Abstractions;

namespace Fdw.Services.Connections;

/// <summary>
/// Configuration class for data container fields (columns/properties).
/// Generates the table <c>data.DataContainerField</c> as a child of <c>data.DataContainer</c>.
/// </summary>
/// <remarks>
/// <para>
/// A field represents a single column or property within a DataContainer.
/// Storage-specific properties (NativeType, IsNullable, Ordinal, MaxLength, Precision, Scale,
/// Collation, IsIdentity, IsComputed, etc.) live on the typed body record
/// <c>data.MsSqlDataContainerField</c> joined by <c>RowId</c>.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration( ServiceCategory = "DataStore",
    ServiceType = "DataContainerField")]
public partial class DataContainerFieldConfiguration : IGenericConfiguration
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DataContainerFieldConfiguration"/> class.
    /// </summary>
    public DataContainerFieldConfiguration()
    {
    }

    /// <summary>
    /// Gets or sets the unique identifier for this field.
    /// </summary>
    public Guid Id { get; set; }


    /// <summary>
    /// Gets or sets the name of this field (column/property name).
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets the section name for configuration binding.
    /// </summary>
    public string SectionName => "DataContainerFields";

    /// <summary>
    /// Gets the service type - always "DataStore" for field configuration.
    /// </summary>
    public string ServiceType => "DataStore";

    /// <summary>
    /// Gets the service option type - null for base field configuration.
    /// </summary>
    public string? ServiceOptionType => null;

    /// <summary>
    /// Gets or sets the DataContainer ID this field belongs to.
    /// </summary>
    public Guid DataContainerId { get; set; }


    /// <summary>
    /// Gets or sets the field's SQL data type (e.g., "String", "Int32", "Decimal").
    /// Maps to <c>data.DataContainerField.DataType</c>.
    /// </summary>
    public string? DataType { get; set; }

    /// <summary>
    /// Gets or sets whether this column is system-provided (IDENTITY, COMPUTED, or DEFAULT-filled
    /// such as NEWSEQUENTIALID()). System-provided columns are excluded from INSERT statements.
    /// Maps to <c>data.DataContainerField.IsSystemProvided</c>.
    /// </summary>
    public bool IsSystemProvided { get; set; }

    /// <summary>
    /// Gets or sets whether this column is visible to the application. <c>Visible=false</c> marks a
    /// DB-only column the app never sees — the physical RowId PK and every <c>{Parent}RowId</c> FK. It
    /// stays in the column metadata so the key metadata can reference it for the in-DB join, but it is
    /// excluded from POCOs/DTOs and app-facing projections. Maps to <c>data.DataContainerField.Visible</c>.
    /// </summary>
    public bool Visible { get; set; } = true;

    /// <summary>
    /// Gets or sets the optional description for this field.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets whether this field allows null values.
    /// JSON-bound from <c>configurationSchema.json</c> — not stored on the base DB table
    /// (lives on the typed body record), but carried here so the schema tree builder
    /// can build typed field nodes without a round-trip to the detail loader.
    /// </summary>
    public bool IsNullable { get; set; }

    /// <summary>
    /// Gets or sets the ordinal position of this field within its container.
    /// JSON-bound from <c>configurationSchema.json</c> — stored on the typed body record.
    /// Used by the schema tree builder to order fields without a detail-loader round-trip.
    /// </summary>
    public int Ordinal { get; set; }

    // ── Type facets: what THIS field is, as opposed to what its type permits ────────────────────
    //
    // Why these are separate from the data type's own MaxLength/MaxPrecision/MaxScale: the type states
    // a LIMIT ("varchar accepts up to 8000"), the field states an INSTANCE ("this column is
    // varchar(50)"). Both are needed and neither can be derived from the other.
    //
    // Why they are being added now rather than having always existed: data.DataContainerField has
    // carried MaxLength, Precision, Scale and DefaultValue since the DDL was written, and this POCO
    // mapped NONE of them. The save translator builds its column list from the intersection of mapper
    // properties and container fields, so all four were omitted from every INSERT and took the DB
    // default. Schema discovery wrote nvarchar with no length, and any field that did carry one lost it
    // on the next version-on-write — 77 rows in devConfigurationDb were in exactly that state.

    /// <summary>
    /// Gets or sets this field's declared length, or null when the type takes none.
    /// Maps to <c>data.DataContainerField.MaxLength</c>.
    /// </summary>
    /// <remarks>
    /// Counted in characters for a Unicode type and in bytes otherwise — the type says which, via
    /// <c>IMsSqlDataType.IsUnicode</c>.
    /// </remarks>
    public int? MaxLength { get; set; }

    /// <summary>
    /// Gets or sets this field's declared precision, or null when the type takes none.
    /// Maps to <c>data.DataContainerField.Precision</c>.
    /// </summary>
    public int? Precision { get; set; }

    /// <summary>
    /// Gets or sets this field's declared scale, or null when the type takes none.
    /// Maps to <c>data.DataContainerField.Scale</c>.
    /// </summary>
    public int? Scale { get; set; }

    /// <summary>
    /// Gets or sets the field's default value expression as the store declares it, or null when it has
    /// none. Maps to <c>data.DataContainerField.DefaultValue</c>.
    /// </summary>
    /// <remarks>
    /// Held as the store's own text (e.g. <c>(getutcdate())</c>) rather than a parsed value: it is a
    /// server-side expression, and parsing it would mean inventing a representation for something only
    /// the backend evaluates.
    /// </remarks>
    public string? DefaultValue { get; set; }

    /// <summary>
    /// Gets or sets the description as the SOURCE system states it, or null when it supplies none.
    /// Maps to <c>data.DataContainerField.SourceDescription</c>.
    /// </summary>
    /// <remarks>
    /// Why this is separate from <see cref="Description"/>: Description is the user's, and re-running
    /// discovery must not overwrite what a person wrote. SourceDescription is the backend's own text
    /// (a SQL Server extended property, an OpenAPI summary), so discovery owns it and can refresh it
    /// freely. The same pair already exists on DataPathConfiguration and on the container DTOs; the
    /// field POCO simply never mapped its half, so a column comment discovered from the source had
    /// nowhere to land.
    /// </remarks>
    public string? SourceDescription { get; set; }

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
