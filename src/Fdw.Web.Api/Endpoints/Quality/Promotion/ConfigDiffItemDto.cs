namespace Fdw.Services.Quality.Endpoints.Promotion;

/// <summary>
/// DTO representing a single property difference between environments.
/// </summary>
public class ConfigDiffItemDto
{
    /// <summary>Gets or sets the property path.</summary>
    public string PropertyPath { get; set; } = string.Empty;

    /// <summary>Gets or sets the value in the source environment.</summary>
    public string? SourceValue { get; set; }

    /// <summary>Gets or sets the value in the target environment.</summary>
    public string? TargetValue { get; set; }

    /// <summary>Gets or sets the diff type (e.g., "Added", "Modified", "Removed").</summary>
    public string DiffType { get; set; } = string.Empty;
}
