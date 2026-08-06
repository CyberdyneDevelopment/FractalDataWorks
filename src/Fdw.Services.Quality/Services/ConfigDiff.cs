using System.Collections.Generic;

namespace Fdw.Services.Quality.Services;

/// <summary>
/// Configuration diff between environments.
/// </summary>
public sealed record ConfigDiff(
    string SourceEnvironment,
    string TargetEnvironment,
    string EntityType,
    string EntityName,
    IReadOnlyList<ConfigDiffItem> Differences);