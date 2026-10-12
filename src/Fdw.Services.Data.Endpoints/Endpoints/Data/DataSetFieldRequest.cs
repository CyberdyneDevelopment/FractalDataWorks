namespace Fdw.Services.Data.Endpoints;

/// <summary>Identifies a data set field by the names in its route.</summary>
public class DataSetFieldRequest
{
    /// <summary>Gets or sets the data set name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the field name within the data set.</summary>
    public string Field { get; set; } = string.Empty;
}
