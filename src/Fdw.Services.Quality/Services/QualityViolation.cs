namespace Fdw.Services.Quality.Services;

/// <summary>
/// A single quality rule violation.
/// </summary>
public sealed record QualityViolation(
    int RecordIndex,
    string? FieldName,
    object? ActualValue,
    string Message);