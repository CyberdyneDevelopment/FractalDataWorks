namespace Fdw.Operations.Endpoints.ConfigurationMetadata;

/// <summary>
/// Request for getting child configuration types.
/// </summary>
public class GetChildTypesRequest
{
    /// <summary>
    /// Gets or sets the parent table name (from query string).
    /// </summary>
    public string Parent { get; set; } = string.Empty;
}
