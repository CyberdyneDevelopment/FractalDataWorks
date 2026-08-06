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
}
