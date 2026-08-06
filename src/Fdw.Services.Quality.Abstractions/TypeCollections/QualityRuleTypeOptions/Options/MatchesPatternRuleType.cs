using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Quality.Abstractions.TypeCollections.QualityRuleTypeOptions.Options;

/// <summary>
/// Rule type that validates a field value matches a regular expression pattern.
/// </summary>
[TypeOption(typeof(QualityRuleTypes), "MatchesPattern")]
[ExcludeFromCodeCoverage]
public sealed class MatchesPatternRuleType : QualityRuleTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MatchesPatternRuleType"/> class.
    /// </summary>
    public MatchesPatternRuleType()
        : base(
            id: 4,
            name: "MatchesPattern",
            requiresField: true,
            supportsMultipleFields: false,
            requiresParameters: true,
            description: "Value must match regex pattern")
    {
    }
}
