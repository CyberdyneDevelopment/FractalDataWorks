namespace Fdw.Web.Clients.Abstractions.Contracts;

/// <summary>
/// Abstraction for column schema metadata used across Schema and Data domains.
/// </summary>
public interface IColumnSchema
{
    /// <summary>Gets the column name.</summary>
    string Name { get; }
    /// <summary>Gets the data type.</summary>
    string DataType { get; }
    /// <summary>Gets a value indicating whether the column allows nulls.</summary>
    bool IsNullable { get; }
    /// <summary>Gets the maximum length.</summary>
    int? MaxLength { get; }
    /// <summary>Gets the numeric precision.</summary>
    int? Precision { get; }
    /// <summary>Gets the numeric scale.</summary>
    int? Scale { get; }
    /// <summary>Gets the inferred property role.</summary>
    string? Role { get; }
}
