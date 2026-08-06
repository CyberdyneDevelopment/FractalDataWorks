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

    /// <summary>Gets or sets whether the field is visible to the application.</summary>
    public bool Visible { get; set; } = true;

    /// <summary>Gets or sets the optional field description.</summary>
    public string? Description { get; set; }
}
