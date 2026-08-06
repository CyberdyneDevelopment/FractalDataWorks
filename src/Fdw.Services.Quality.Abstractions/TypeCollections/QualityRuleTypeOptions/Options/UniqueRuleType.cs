using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Quality.Abstractions.TypeCollections.QualityRuleTypeOptions.Options;

/// <summary>
/// Rule type that validates field values are unique across the dataset.
/// </summary>
[TypeOption(typeof(QualityRuleTypes), "Unique")]
[ExcludeFromCodeCoverage]
public sealed class UniqueRuleType : QualityRuleTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UniqueRuleType"/> class.
    /// </summary>
    public UniqueRuleType()
        : base(
            id: 2,
            name: "Unique",
            requiresField: true,
            supportsMultipleFields: false,
            requiresParameters: false,
            description: "Field values must be unique")
    {
    }
}
