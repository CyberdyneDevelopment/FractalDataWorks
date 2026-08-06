using System.Collections.Generic;

namespace Fdw.Services.Quality.Endpoints.Promotion;

/// <summary>
/// DTO representing configuration differences between two environments.
/// </summary>
public class ConfigDiffDto
{
    /// <summary>Gets or sets the source environment name.</summary>
    public string SourceEnvironment { get; set; } = string.Empty;

    /// <summary>Gets or sets the target environment name.</summary>
    public string TargetEnvironment { get; set; } = string.Empty;

    /// <summary>Gets or sets the entity type compared.</summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>Gets or sets the entity name compared.</summary>
    public string EntityName { get; set; } = string.Empty;

    /// <summary>Gets or sets the individual property differences.</summary>
    public IReadOnlyList<ConfigDiffItemDto> Differences { get; set; } = [];
}
