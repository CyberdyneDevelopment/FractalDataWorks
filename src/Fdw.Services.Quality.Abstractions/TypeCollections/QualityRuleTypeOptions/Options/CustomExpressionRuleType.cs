using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Quality.Abstractions.TypeCollections.QualityRuleTypeOptions.Options;

/// <summary>
/// Rule type that validates a field using a custom C# expression.
/// </summary>
[TypeOption(typeof(QualityRuleTypes), "CustomExpression")]
[ExcludeFromCodeCoverage]
public sealed class CustomExpressionRuleType : QualityRuleTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CustomExpressionRuleType"/> class.
    /// </summary>
    public CustomExpressionRuleType()
        : base(
            id: 6,
            name: "CustomExpression",
            requiresField: true,
            supportsMultipleFields: false,
            requiresParameters: true,
            description: "Value must satisfy custom C# expression")
    {
    }
}
