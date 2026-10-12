namespace Fdw.Web.Clients.Abstractions.Contracts;

using System;

/// <summary>
/// Abstraction for DataSet field metadata used across Data and Calculations domains.
/// </summary>
public interface IDataSetField
{
    /// <summary>Gets the unique identifier.</summary>
    Guid Id { get; }
    /// <summary>Gets the field name.</summary>
    string Name { get; }
    /// <summary>Gets the description.</summary>
    string? Description { get; }
    /// <summary>Gets the data type.</summary>
    string DataType { get; }
    /// <summary>Gets whether the field is a key.</summary>
    bool IsKey { get; }
    /// <summary>Gets whether the field is required.</summary>
    bool IsRequired { get; }
    /// <summary>Gets whether the field is indexed.</summary>
    bool IsIndexed { get; }
    /// <summary>Gets the maximum length.</summary>
    int? MaxLength { get; }
    /// <summary>Gets the default value.</summary>
    string? DefaultValue { get; }
    /// <summary>Gets whether the field is calculated.</summary>
    bool IsCalculated { get; }
    /// <summary>Gets the property role.</summary>
    string? Role { get; }
    /// <summary>Gets the ordinal position.</summary>
    int Ordinal { get; }
}
