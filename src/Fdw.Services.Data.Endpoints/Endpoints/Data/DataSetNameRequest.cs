namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Request DTO for operations that require a data set name.
/// </summary>
public class DataSetNameRequest
{
    /// <summary>Gets or sets the data set name.</summary>
    public string Name { get; set; } = string.Empty;
}
