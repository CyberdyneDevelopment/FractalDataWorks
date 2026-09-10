using Fdw.Services.Quality.Configuration;

namespace Fdw.Services.Quality.Endpoints;

/// <summary>Maps a quality-rule request onto the configuration that is written.</summary>
/// <remarks>
/// On the endpoint side rather than the provider: shaping a request into a configuration is what
/// an endpoint does, and a provider carries no logic of its own.
/// </remarks>
internal static class QualityRuleRequestMapper
{
    /// <summary>Builds a rule configuration from the parts a request carries.</summary>
    internal static QualityRuleImplementationConfiguration FromRequest(
        string dataSetName, string? fieldName, string ruleType, string severity,
        bool isEnabled, string? description, string? minValue, string? maxValue,
        string? pattern, string? expression, string? name = null)
        => new()
        {
            Name = string.IsNullOrWhiteSpace(name) ? $"{dataSetName}:{ruleType}" : name,
            DataSetName = dataSetName,
            FieldName = fieldName,
            RuleType = ruleType,
            Severity = severity,
            IsEnabled = isEnabled,
            Description = description,
            MinValue = minValue,
            MaxValue = maxValue,
            Pattern = pattern,
            Expression = expression
        };
}
