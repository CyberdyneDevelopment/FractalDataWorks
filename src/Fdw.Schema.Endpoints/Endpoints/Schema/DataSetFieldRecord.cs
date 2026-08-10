using System;

namespace Fdw.Schema.Endpoints;

/// <summary>
/// Database record representing a field within a data set.
/// </summary>
public class DataSetFieldRecord
{
    /// <summary>Gets or sets the field record identifier.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the parent data set identifier.</summary>
    public Guid DataSetId { get; set; }
    /// <summary>Gets or sets the field name.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Gets or sets the fully qualified type name of the field.</summary>
    public string TypeName { get; set; } = string.Empty;
    /// <summary>Gets or sets the field role.</summary>
    public string? Role { get; set; }
    /// <summary>Gets or sets whether the field is required.</summary>
    public bool IsRequired { get; set; }
    /// <summary>Gets or sets the maximum length for string fields.</summary>
    public int? MaxLength { get; set; }
    /// <summary>Gets or sets the field description.</summary>
    public string? Description { get; set; }
    /// <summary>Gets or sets the ordinal position of the field.</summary>
    public int Ordinal { get; set; }
}
