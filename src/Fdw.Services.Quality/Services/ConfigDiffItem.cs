namespace Fdw.Services.Quality.Services;

/// <summary>
/// A single configuration difference.
/// </summary>
public sealed record ConfigDiffItem(
    string PropertyPath,
    object? SourceValue,
    object? TargetValue,
    string DiffType);