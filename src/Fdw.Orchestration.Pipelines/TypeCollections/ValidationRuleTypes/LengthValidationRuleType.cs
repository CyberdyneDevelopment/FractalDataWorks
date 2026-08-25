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
/// Validation rule that checks if string values are within length constraints.
/// </summary>
[TypeOption(typeof(ValidationRuleTypesCollection), "Length", RestrictToCurrentCompilation = true)]
public sealed class LengthValidationRuleType : ValidationRuleTypeBase
{
    private static readonly IReadOnlyList<string> ParameterNames = new[] { "MinLength", "MaxLength" };

    /// <summary>
    /// Initializes a new instance of the <see cref="LengthValidationRuleType"/> class.
    /// </summary>
    public LengthValidationRuleType()
        : base(
            id: 4,
            name: "Length",
            requiresFields: true,
            supportsMultipleFields: true,
            requiresParameters: true,
            requiredParameterNames: ParameterNames)
    {
    }

    /// <inheritdoc/>
    public override Task<IGenericResult<ValidationRuleResult>> Validate(
        IReadOnlyDictionary<string, object?> record,
        IReadOnlyList<string> fields,
        IReadOnlyDictionary<string, object?> parameters,
        CancellationToken cancellationToken = default)
    {
        var errors = new Dictionary<string, string>(StringComparer.Ordinal);

        parameters.TryGetValue("MinLength", out var minObj);
        parameters.TryGetValue("MaxLength", out var maxObj);

        var minLength = minObj != null ? Convert.ToInt32(minObj, System.Globalization.CultureInfo.InvariantCulture) : 0;
        var maxLength = maxObj != null ? Convert.ToInt32(maxObj, System.Globalization.CultureInfo.InvariantCulture) : int.MaxValue;

        foreach (var field in fields)
        {
            if (record.TryGetValue(field, out var value) && value != null)
            {
                var stringValue = value.ToString() ?? string.Empty;
                var length = stringValue.Length;

                if (length < minLength)
                {
                    errors[field] = $"Field '{field}' length {length} is less than minimum {minLength}";
                }
                else if (length > maxLength)
                {
                    errors[field] = $"Field '{field}' length {length} exceeds maximum {maxLength}";
                }
            }
        }

        if (errors.Count > 0)
        {
            return Task.FromResult<IGenericResult<ValidationRuleResult>>(
                GenericResult<ValidationRuleResult>.Success(
                    ValidationRuleResult.Failure("Length validation failed", errors)));
        }

        return Task.FromResult<IGenericResult<ValidationRuleResult>>(
            GenericResult<ValidationRuleResult>.Success(ValidationRuleResult.Success()));
    }
}
