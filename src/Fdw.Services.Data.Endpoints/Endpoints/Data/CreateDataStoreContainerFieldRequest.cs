using Fdw.Data.Abstractions;
using Fdw.Schema;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Request DTO for a field within a data store container creation payload.
/// Mirrors the caller-supplied columns of <see cref="Fdw.Services.Connections.DataContainerFieldConfiguration"/>.
/// </summary>
public class CreateDataStoreContainerFieldRequest
{
    /// <summary>Gets or sets the field (column) name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the field type discriminator (e.g., "String", "Int32", "Decimal").</summary>
    public string DataType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the portable type the field normalizes to: the name of a <c>DataTypes</c> option, listed by
    /// <c>GET /data-types</c> (FDW-1103). Required: a field a person declares states it, and a name that is not
    /// an option is refused.
    /// </summary>
    public string FrameworkDataType { get; set; } = string.Empty;

    /// <summary>Gets or sets whether the field allows null values.</summary>
    public bool IsNullable { get; set; }

    /// <summary>Gets or sets the ordinal position of the field within the container.</summary>
    public int Ordinal { get; set; }

    /// <summary>
    /// Gets or sets whether this column is system-provided (IDENTITY, COMPUTED, or DEFAULT-filled such
    /// as NEWSEQUENTIALID()). System-provided columns are excluded from INSERT statements, so a caller
    /// must mark RowId/audit-style columns true to keep bulk-insert from targeting them.
    /// </summary>
    public bool IsSystemProvided { get; set; }

    /// <summary>
    /// Gets or sets the <c>DatumTypeId</c> of what the field's data is (FDW-988). Left out, it is
    /// <c>NotYetTyped</c>. A physical key field is <c>DbKey</c>: the key definition names it, and no
    /// response returns it. An id that is not a datum type is refused.
    /// </summary>
    public int DatumTypeId { get; set; } = DatumTypes.NotYetTyped.Id;

    /// <summary>Gets or sets the optional field description.</summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the <c>ClassificationId</c> of the field's data classification level (see
    /// <c>GET /data-classifications</c>). Left out, it is <c>NotYetClassified</c>. An id that is not a level is refused.
    /// </summary>
    public int ClassificationId { get; set; } = DataClassifications.NotYetClassified.Id;

    /// <summary>Gets or sets this field's declared length, or null when the type takes none.</summary>
    public int? MaxLength { get; set; }

    /// <summary>Gets or sets this field's declared precision, or null when the type takes none.</summary>
    public int? Precision { get; set; }

    /// <summary>Gets or sets this field's declared scale, or null when the type takes none.</summary>
    public int? Scale { get; set; }
}
