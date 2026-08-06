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
/// Validation rule that checks if required fields have values.
/// </summary>
[TypeOption(typeof(ValidationRuleTypesCollection), "Required", RestrictToCurrentCompilation = true)]
public sealed class RequiredValidationRuleType : ValidationRuleTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RequiredValidationRuleType"/> class.
    /// </summary>
    public RequiredValidationRuleType()
        : base(
            id: 1,
            name: "Required",
            requiresFields: true,
            supportsMultipleFields: true,
            requiresParameters: false)
    {
    }

    /// <inheritdoc/>
    public override Task<IGenericResult<ValidationResult>> Validate(
        IReadOnlyDictionary<string, object?> record,
        IReadOnlyList<string> fields,
        IReadOnlyDictionary<string, object?> parameters,
        CancellationToken cancellationToken = default)
    {
        var errors = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var field in fields)
        {
            if (!record.TryGetValue(field, out var value) || value == null || (value is string s && string.IsNullOrWhiteSpace(s)))
            {
                errors[field] = $"Field '{field}' is required";
            }
        }

        if (errors.Count > 0)
        {
            return Task.FromResult<IGenericResult<ValidationResult>>(
                GenericResult<ValidationResult>.Success(
                    ValidationResult.Failure("Required field validation failed", errors)));
        }

        return Task.FromResult<IGenericResult<ValidationResult>>(
            GenericResult<ValidationResult>.Success(ValidationResult.Success()));
    }
}
