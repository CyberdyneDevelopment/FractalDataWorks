using System;

namespace Fdw.Services.Data.Clients.Models;

/// <summary>
/// Represents a field within a container.
/// </summary>
public sealed class DataStoreFieldPayload
{
    /// <summary>Gets or sets the unique identifier.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the field name.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Gets or sets the native (storage-layer) data type; populated when the server maps fields from a physical container.</summary>
    public string? NativeDataType { get; set; }
    /// <summary>Gets or sets a value indicating whether the field allows null values.</summary>
    public bool IsNullable { get; set; }
    /// <summary>Gets or sets a value indicating whether this is a key field.</summary>
    public bool IsKey { get; set; }
    /// <summary>Gets or sets the ordinal position of the field.</summary>
    public int Ordinal { get; set; }
    /// <summary>Gets or sets the maximum length, if applicable.</summary>
    public int? MaxLength { get; set; }
    /// <summary>Gets or sets the numeric precision, if applicable.</summary>
    public int? Precision { get; set; }
    /// <summary>Gets or sets the numeric scale, if applicable.</summary>
    public int? Scale { get; set; }
}
