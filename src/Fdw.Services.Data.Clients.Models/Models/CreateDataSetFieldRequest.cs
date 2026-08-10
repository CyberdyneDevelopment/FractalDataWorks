namespace Fdw.Services.Data.Clients.Models;

/// <summary>
/// Request to create a DataSet field.
/// </summary>
public sealed class CreateDataSetFieldRequest
{
    /// <summary>Gets or sets the name.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Gets or sets the description.</summary>
    public string? Description { get; set; }
    /// <summary>Gets or sets the data type.</summary>
    public string DataType { get; set; } = string.Empty;
    /// <summary>Gets or sets whether the field is a key.</summary>
    public bool IsKey { get; set; }
    /// <summary>Gets or sets whether the field is required.</summary>
    public bool IsRequired { get; set; }
    /// <summary>Gets or sets whether the field is indexed.</summary>
    public bool IsIndexed { get; set; }
    /// <summary>Gets or sets the maximum length.</summary>
    public int? MaxLength { get; set; }
    /// <summary>Gets or sets the default value.</summary>
    public string? DefaultValue { get; set; }
    /// <summary>Gets or sets whether the field is calculated.</summary>
    public bool IsCalculated { get; set; }
    /// <summary>Gets or sets the name of the configured calculation that computes this field's value when <see cref="IsCalculated"/> is true.</summary>
    public string? CalculationName { get; set; }
    /// <summary>Gets or sets whether this field participates as a join key for cross-source joins.</summary>
    public bool IsJoinKey { get; set; }
    /// <summary>Gets or sets the property role.</summary>
    public string? Role { get; set; }
    /// <summary>Gets or sets the ordinal position.</summary>
    public int Ordinal { get; set; }
}
