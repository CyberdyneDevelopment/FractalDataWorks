using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Quality.Abstractions.TypeCollections.QualityRuleTypeOptions.Options;

/// <summary>
/// Rule type that validates a field value is within specified minimum and maximum bounds.
/// </summary>
[TypeOption(typeof(QualityRuleTypes), "InRange")]
[ExcludeFromCodeCoverage]
public sealed class InRangeRuleType : QualityRuleTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InRangeRuleType"/> class.
    /// </summary>
    public InRangeRuleType()
        : base(
            id: 3,
            name: "InRange",
            requiresField: true,
            supportsMultipleFields: false,
            requiresParameters: true,
            description: "Value must be within min/max bounds")
    {
    }
}
