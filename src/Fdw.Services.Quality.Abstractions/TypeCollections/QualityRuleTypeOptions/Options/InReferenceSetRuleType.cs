using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Quality.Abstractions.TypeCollections.QualityRuleTypeOptions.Options;

/// <summary>
/// Rule type that validates a field value exists in an allowed set of values.
/// </summary>
[TypeOption(typeof(QualityRuleTypes), "InReferenceSet")]
[ExcludeFromCodeCoverage]
public sealed class InReferenceSetRuleType : QualityRuleTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InReferenceSetRuleType"/> class.
    /// </summary>
    public InReferenceSetRuleType()
        : base(
            id: 5,
            name: "InReferenceSet",
            requiresField: true,
            supportsMultipleFields: false,
            requiresParameters: true,
            description: "Value must be in allowed set")
    {
    }
}
