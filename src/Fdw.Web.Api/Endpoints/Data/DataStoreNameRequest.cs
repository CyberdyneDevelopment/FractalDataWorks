namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Request DTO for operations that require a data store name.
/// </summary>
public class DataStoreNameRequest
{
    /// <summary>Gets or sets the data store name.</summary>
    public string Name { get; set; } = string.Empty;
}
