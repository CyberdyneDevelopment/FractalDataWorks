namespace Fdw.Web.Clients.Abstractions.Contracts;

/// <summary>
/// Abstraction for field discovery results used across Schema and Data domains.
/// </summary>
public interface IFieldDiscovery
{
    /// <summary>Gets the name of the discovered field.</summary>
    string Name { get; }
    /// <summary>Gets the data type of the discovered field.</summary>
    string DataType { get; }
    /// <summary>Gets a value indicating whether the field allows null values.</summary>
    bool IsNullable { get; }
    /// <summary>Gets a value indicating whether the field is a key.</summary>
    bool IsKey { get; }
}
