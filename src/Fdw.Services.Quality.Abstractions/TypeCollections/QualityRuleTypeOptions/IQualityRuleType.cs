using Fdw.Collections;

namespace Fdw.Services.Quality.Abstractions.TypeCollections.QualityRuleTypeOptions;

/// <summary>
/// Represents a quality rule type that defines validation logic for data quality checks.
/// </summary>
public interface IQualityRuleType : ITypeOption<int, QualityRuleTypeBase>
{
    /// <summary>
    /// Gets a value indicating whether this rule type requires a specific field to operate on.
    /// </summary>
    bool RequiresField { get; }

    /// <summary>
    /// Gets a value indicating whether this rule type supports multiple fields.
    /// </summary>
    bool SupportsMultipleFields { get; }

    /// <summary>
    /// Gets a value indicating whether this rule type requires additional parameters.
    /// </summary>
    bool RequiresParameters { get; }

    /// <summary>
    /// Gets the human-readable description of what this rule type validates.
    /// </summary>
    string Description { get; }
}
