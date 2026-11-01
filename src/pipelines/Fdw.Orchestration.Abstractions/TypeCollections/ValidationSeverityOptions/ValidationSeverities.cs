using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Orchestration.Abstractions.TypeCollections.ValidationSeverityOptions;

/// <summary>
/// TypeCollection for validation severities.
/// </summary>
/// <remarks>
/// Provides compile-time discovery and O(1) lookup for validation severities.
/// Source generator creates static properties for each registered validation severity.
/// </remarks>
[TypeCollection(typeof(ValidationSeverityBase), typeof(IValidationSeverity), typeof(ValidationSeverities))]
public sealed partial class ValidationSeverities : TypeCollectionBase<ValidationSeverityBase, IValidationSeverity>
{
}
