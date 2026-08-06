using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Orchestration.Pipelines.Abstractions.TypeCollections.ValidationRuleTypeOptions;
using Fdw.Orchestration.Pipelines.Results;
using Fdw.Results;
using ValidationRuleTypesCollection = Fdw.Orchestration.Pipelines.Abstractions.TypeCollections.ValidationRuleTypeOptions.ValidationRuleTypes;

namespace Fdw.Orchestration.Pipelines.TypeCollections.ValidationRuleTypes;

/// <summary>
/// Validation rule for custom validation logic.
/// </summary>
/// <remarks>
/// Custom validation allows users to provide their own validation logic
/// via a delegate or expression in the parameters.
/// </remarks>
[TypeOption(typeof(ValidationRuleTypesCollection), "Custom", RestrictToCurrentCompilation = true)]
public sealed class CustomValidationRuleType : ValidationRuleTypeBase
{
    private static readonly IReadOnlyList<string> ParameterNames = new[] { "Validator" };

    /// <summary>
    /// Initializes a new instance of the <see cref="CustomValidationRuleType"/> class.
    /// </summary>
    public CustomValidationRuleType()
        : base(
            id: 8,
            name: "Custom",
            requiresFields: false,
            supportsMultipleFields: true,
            requiresParameters: true,
            requiredParameterNames: ParameterNames)
    {
    }

    /// <inheritdoc/>
    public override Task<IGenericResult<ValidationResult>> Validate(
        IReadOnlyDictionary<string, object?> record,
        IReadOnlyList<string> fields,
        IReadOnlyDictionary<string, object?> parameters,
        CancellationToken cancellationToken = default)
    {
        if (!parameters.TryGetValue("Validator", out var validatorObj))
        {
            return Task.FromResult<IGenericResult<ValidationResult>>(
                GenericResult<ValidationResult>.Failure(PipelineResultCodes.ByName("ValidatorParameterRequired")));
        }

        // Support different validator types
        if (validatorObj is Func<IReadOnlyDictionary<string, object?>, ValidationResult> syncValidator)
        {
            var result = syncValidator(record);
            return Task.FromResult<IGenericResult<ValidationResult>>(
                GenericResult<ValidationResult>.Success(result));
        }

        if (validatorObj is Func<IReadOnlyDictionary<string, object?>, Task<ValidationResult>> asyncValidator)
        {
            return ExecuteAsyncValidator(asyncValidator, record);
        }

        return Task.FromResult<IGenericResult<ValidationResult>>(
            GenericResult<ValidationResult>.Failure(PipelineResultCodes.ByName("InvalidValidatorType")));
    }

    private static async Task<IGenericResult<ValidationResult>> ExecuteAsyncValidator(
        Func<IReadOnlyDictionary<string, object?>, Task<ValidationResult>> validator,
        IReadOnlyDictionary<string, object?> record)
    {
        var result = await validator(record).ConfigureAwait(false);
        return GenericResult<ValidationResult>.Success(result);
    }
}
