namespace Fdw.Operations.Endpoints.ConfigurationMetadata;

/// <summary>
/// Request for listing configuration types by category.
/// </summary>
public class GetTypesByCategoryRequest
{
    /// <summary>
    /// Gets or sets the category filter (from query string).
    /// </summary>
    public string Category { get; set; } = string.Empty;
}
