using System;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// DTO representing a field within a data container.
/// </summary>
public class DataStoreFieldResponse
{
    /// <summary>Gets or sets the field identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the field name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the native data type.</summary>
    public string? NativeDataType { get; set; }

    /// <summary>Gets or sets the framework data type.</summary>
    public string? FrameworkDataType { get; set; }

    /// <summary>Gets or sets whether the field is nullable.</summary>
    public bool IsNullable { get; set; }

    /// <summary>Gets or sets whether the field is a key field.</summary>
    public bool IsKey { get; set; }

    /// <summary>Gets or sets the field's ordinal position.</summary>
    public int Ordinal { get; set; }

    /// <summary>Gets or sets the field description.</summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the <c>ClassificationId</c> of the field's data classification level; <c>NotYetClassified</c> until someone picks a level.
    /// </summary>
    public int ClassificationId { get; set; }

    /// <summary>
    /// Gets or sets the <c>DatumTypeId</c> of what the field's data is (see <c>GET /datum-types</c>);
    /// <c>NotYetTyped</c> until someone picks one (FDW-988).
    /// </summary>
    public int DatumTypeId { get; set; }

    /// <summary>Gets or sets this field's declared length, or null when the type takes none.</summary>
    public int? MaxLength { get; set; }

    /// <summary>Gets or sets this field's declared precision, or null when the type takes none.</summary>
    public int? Precision { get; set; }

    /// <summary>Gets or sets this field's declared scale, or null when the type takes none.</summary>
    public int? Scale { get; set; }
}
