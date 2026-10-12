using Fdw.Web.Clients.Abstractions.Contracts;

namespace Fdw.Services.Data.Clients.Models;

/// <summary>
/// Result of field discovery.
/// </summary>
public sealed class FieldDiscoveryResult : IFieldDiscovery
{
    /// <summary>Gets or sets the name of the field discovered.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Gets or sets the data type of the field discovered.</summary>
    public string DataType { get; set; } = string.Empty;
    /// <summary>Gets or sets a value indicating whether the field discovered allows null values.</summary>
    public bool IsNullable { get; set; }
    /// <summary>Gets or sets a value indicating whether the field discovered is a key.</summary>
    public bool IsKey { get; set; }
}
