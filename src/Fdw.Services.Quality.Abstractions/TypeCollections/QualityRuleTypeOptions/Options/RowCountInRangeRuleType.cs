using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Quality.Abstractions.TypeCollections.QualityRuleTypeOptions.Options;

/// <summary>
/// Rule type that validates the dataset row count is within specified bounds.
/// This is an aggregate rule that operates on the entire dataset.
/// </summary>
[TypeOption(typeof(QualityRuleTypes), "RowCountInRange")]
[ExcludeFromCodeCoverage]
public sealed class RowCountInRangeRuleType : QualityRuleTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RowCountInRangeRuleType"/> class.
    /// </summary>
    public RowCountInRangeRuleType()
        : base(
            id: 10,
            name: "RowCountInRange",
            requiresField: false,
            supportsMultipleFields: false,
            requiresParameters: true,
            description: "Dataset row count must be within bounds")
    {
    }
}
