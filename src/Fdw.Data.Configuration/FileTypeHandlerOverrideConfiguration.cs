using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Services.Connections;

/// <summary>
/// Configuration class for per-DataPath file-type handler overrides.
/// Maps to the <c>data.FileTypeHandlerOverride</c> table.
/// </summary>
/// <remarks>
/// <para>
/// A FileTypeHandlerOverride causes a specific file extension on a given DataPath to
/// use a named <c>IFileTypeHandler</c> (from the <c>FileTypeHandlers</c> TypeCollection)
/// instead of the system-wide default chosen by extension. Overrides are consulted
/// BEFORE the default handler lookup so per-path configuration wins.
/// </para>
/// <para>
/// The FK from this table to <c>data.DataPath</c> is physical: <c>DataPathRowId</c> joins
/// to <c>data.DataPath.RowId</c>. This means the parent must be looked up via Physical key.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "DataStore", ServiceType = "FileTypeHandlerOverride")]
public partial class FileTypeHandlerOverrideConfiguration : IGenericConfiguration
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FileTypeHandlerOverrideConfiguration"/> class.
    /// </summary>
    public FileTypeHandlerOverrideConfiguration()
    {
    }

    /// <summary>
    /// Gets or sets the unique logical identifier for this override row.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the parent DataPath's logical Id (FK to data.DataPath.Id). The physical DataPathRowId
    /// is DB-managed and invisible — the save translator resolves it from this Id on insert.
    /// </summary>
    public Guid DataPathId { get; set; }

    /// <summary>
    /// Gets or sets the display name for this override entry.
    /// </summary>
    /// <remarks>
    /// Why: IGenericConfiguration requires Name. data.FileTypeHandlerOverride has no Name column —
    /// the record is identified by DataPathRowId + Extension. This property is [NotMapped]
    /// so the source generator does not emit a Name column in DDL.
    /// </remarks>
    [NotMapped]
    public string Name { get; set; } = string.Empty;


    /// <summary>
    /// Gets the section name for IOptions binding.
    /// </summary>
    public string SectionName => "FileTypeHandlerOverrides";

    /// <summary>
    /// Gets the service type — always "DataStore" for child config of DataPath.
    /// </summary>
    public string ServiceType => "DataStore";

    /// <summary>
    /// Gets the service option type — always "FileTypeHandlerOverride".
    /// </summary>
    public string? ServiceOptionType => "FileTypeHandlerOverride";


    /// <summary>
    /// Gets or sets the file extension this override targets (e.g., ".csv", ".json", ".parquet").
    /// Should include the leading dot.
    /// </summary>
    public string Extension { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the <c>IFileTypeHandler</c> to use for files matching
    /// <see cref="Extension"/> on this DataPath. Must match a TypeOption registered in
    /// <c>FileTypeHandlers</c>.
    /// </summary>
    public string HandlerName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional tenant scope for this override.
    /// When set, this override applies only to requests whose <c>IRequestContext.TenantId</c>
    /// matches this value.
    /// </summary>
    public Guid? TenantId { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this is the current (active) version of the row.
    /// </summary>
    public bool IsCurrent { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this row has been soft-deleted.
    /// </summary>
    public bool IsDeleted { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the row was created.
    /// </summary>
    public DateTimeOffset CreateDate { get; set; }

    /// <summary>
    /// Gets or sets the database user who created the row.
    /// </summary>
    public string CreateBy { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timestamp when the row was last modified.
    /// </summary>
    public DateTimeOffset ModifyDate { get; set; }

    /// <summary>
    /// Gets or sets the database user who last modified the row.
    /// </summary>
    public string ModifyBy { get; set; } = string.Empty;
}
