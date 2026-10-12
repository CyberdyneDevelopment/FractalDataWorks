using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Orchestration.Pipelines.Abstractions.TypeCollections.ValidationRuleTypeOptions;

/// <summary>
/// TypeCollection for validation rule types.
/// </summary>
/// <remarks>
/// Provides compile-time discovery and O(1) lookup for validation rule types.
/// Source generator creates static properties for each registered rule type.
/// </remarks>
[TypeCollection(typeof(ValidationRuleTypeBase), typeof(IValidationRuleType), typeof(ValidationRuleTypes))]
public sealed partial class ValidationRuleTypes : TypeCollectionBase<ValidationRuleTypeBase, IValidationRuleType>
{
}
