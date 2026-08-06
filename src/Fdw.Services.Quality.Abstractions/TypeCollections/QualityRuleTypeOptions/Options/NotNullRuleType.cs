using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Quality.Abstractions.TypeCollections.QualityRuleTypeOptions.Options;

/// <summary>
/// Rule type that validates a field is not null.
/// </summary>
[TypeOption(typeof(QualityRuleTypes), "NotNull")]
[ExcludeFromCodeCoverage]
public sealed class NotNullRuleType : QualityRuleTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NotNullRuleType"/> class.
    /// </summary>
    public NotNullRuleType()
        : base(
            id: 1,
            name: "NotNull",
            requiresField: true,
            supportsMultipleFields: false,
            requiresParameters: false,
            description: "Field must not be null")
    {
    }
}
