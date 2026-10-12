using Fdw.Collections;

namespace Fdw.Services.Quality.Abstractions.TypeCollections.QualitySeverityTypeOptions;

/// <summary>
/// Represents a severity level for quality rule violations.
/// </summary>
public interface IQualitySeverityType : ITypeOption<int, QualitySeverityTypeBase>
{
    /// <summary>
    /// Gets the priority level where lower numbers indicate higher priority.
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// Gets a value indicating whether violations of this severity block processing.
    /// </summary>
    bool BlocksProcessing { get; }
}
