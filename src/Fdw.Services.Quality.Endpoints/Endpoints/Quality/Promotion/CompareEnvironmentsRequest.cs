namespace Fdw.Services.Quality.Endpoints.Promotion;

/// <summary>
/// Request to compare configuration between two environments.
/// </summary>
public class CompareEnvironmentsRequest
{
    /// <summary>Gets or sets the source environment name.</summary>
    public string SourceEnvironment { get; set; } = string.Empty;

    /// <summary>Gets or sets the target environment name.</summary>
    public string TargetEnvironment { get; set; } = string.Empty;

    /// <summary>Gets or sets the entity type to compare (e.g., "Pipeline", "Connection").</summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>Gets or sets the entity name to compare.</summary>
    public string EntityName { get; set; } = string.Empty;
}
