using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Orchestration.Pipelines.Abstractions.TypeCollections.ValidationRuleTypeOptions;
using Fdw.Results;
using ValidationRuleTypesCollection = Fdw.Orchestration.Pipelines.Abstractions.TypeCollections.ValidationRuleTypeOptions.ValidationRuleTypes;

namespace Fdw.Orchestration.Pipelines.TypeCollections.ValidationRuleTypes;

/// <summary>
/// Validation rule that checks if field values are unique within a dataset.
/// </summary>
/// <remarks>
/// Unique validation requires access to the full dataset, which is typically
/// handled by the pipeline executor. Single-record validation marks as success
/// and defers uniqueness checking to batch processing.
/// </remarks>
[TypeOption(typeof(ValidationRuleTypesCollection), "Unique", RestrictToCurrentCompilation = true)]
public sealed class UniqueValidationRuleType : ValidationRuleTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UniqueValidationRuleType"/> class.
    /// </summary>
    public UniqueValidationRuleType()
        : base(
            id: 7,
            name: "Unique",
            requiresFields: true,
            supportsMultipleFields: true,
            requiresParameters: false)
    {
    }

    /// <inheritdoc/>
    public override Task<IGenericResult<ValidationRuleResult>> Validate(
        IReadOnlyDictionary<string, object?> record,
        IReadOnlyList<string> fields,
        IReadOnlyDictionary<string, object?> parameters,
        CancellationToken cancellationToken = default)
    {
        // Single-record validation cannot check uniqueness across dataset.
        // This is deferred to batch validation at the pipeline level.
        // Return success here; the pipeline executor handles full uniqueness checks.
        return Task.FromResult<IGenericResult<ValidationRuleResult>>(
            GenericResult<ValidationRuleResult>.Success(
                ValidationRuleResult.Success("Uniqueness deferred to batch validation")));
    }
}
