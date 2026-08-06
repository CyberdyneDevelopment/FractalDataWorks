using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Quality.Abstractions.TypeCollections.QualityRuleTypeOptions.Options;

/// <summary>
/// Rule type that validates the percentage of null values in a field is below a threshold.
/// </summary>
[TypeOption(typeof(QualityRuleTypes), "NullPercentageBelow")]
[ExcludeFromCodeCoverage]
public sealed class NullPercentageBelowRuleType : QualityRuleTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NullPercentageBelowRuleType"/> class.
    /// </summary>
    public NullPercentageBelowRuleType()
        : base(
            id: 11,
            name: "NullPercentageBelow",
            requiresField: true,
            supportsMultipleFields: false,
            requiresParameters: true,
            description: "Null percentage must be below threshold")
    {
    }
}
