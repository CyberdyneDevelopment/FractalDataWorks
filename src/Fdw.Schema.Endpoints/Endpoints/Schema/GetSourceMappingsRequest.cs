namespace Fdw.Schema.Endpoints;

/// <summary>
/// Request for getting mappings by DataSet and source.
/// </summary>
public class GetSourceMappingsRequest
{
    /// <summary>
    /// Gets or sets the DataSet name (from route).
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the source name (from route).
    /// </summary>
    public string SourceName { get; set; } = string.Empty;
}