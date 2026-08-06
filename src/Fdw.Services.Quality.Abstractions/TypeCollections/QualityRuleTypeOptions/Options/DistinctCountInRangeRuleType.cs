using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Quality.Abstractions.TypeCollections.QualityRuleTypeOptions.Options;

/// <summary>
/// Rule type that validates the distinct count (cardinality) of a field is within specified bounds.
/// </summary>
[TypeOption(typeof(QualityRuleTypes), "DistinctCountInRange")]
[ExcludeFromCodeCoverage]
public sealed class DistinctCountInRangeRuleType : QualityRuleTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DistinctCountInRangeRuleType"/> class.
    /// </summary>
    public DistinctCountInRangeRuleType()
        : base(
            id: 12,
            name: "DistinctCountInRange",
            requiresField: true,
            supportsMultipleFields: false,
            requiresParameters: true,
            description: "Distinct value count must be within bounds")
    {
    }
}
