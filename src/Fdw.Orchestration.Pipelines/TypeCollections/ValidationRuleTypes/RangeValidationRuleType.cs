using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Orchestration.Pipelines.Abstractions.TypeCollections.ValidationRuleTypeOptions;
using Fdw.Results;
using ValidationRuleTypesCollection = Fdw.Orchestration.Pipelines.Abstractions.TypeCollections.ValidationRuleTypeOptions.ValidationRuleTypes;

namespace Fdw.Orchestration.Pipelines.TypeCollections.ValidationRuleTypes;

/// <summary>
/// Validation rule that checks if numeric values are within a specified range.
/// </summary>
[TypeOption(typeof(ValidationRuleTypesCollection), "Range", RestrictToCurrentCompilation = true)]
public sealed class RangeValidationRuleType : ValidationRuleTypeBase
{
    private static readonly IReadOnlyList<string> ParameterNames = new[] { "Min", "Max" };

    /// <summary>
    /// Initializes a new instance of the <see cref="RangeValidationRuleType"/> class.
    /// </summary>
    public RangeValidationRuleType()
        : base(
            id: 2,
            name: "Range",
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

        parameters.TryGetValue("Min", out var minObj);
        parameters.TryGetValue("Max", out var maxObj);

        var min = Convert.ToDouble(minObj ?? double.MinValue, System.Globalization.CultureInfo.InvariantCulture);
        var max = Convert.ToDouble(maxObj ?? double.MaxValue, System.Globalization.CultureInfo.InvariantCulture);

        foreach (var field in fields)
        {
            if (record.TryGetValue(field, out var value) && value != null)
            {
                var str = Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty;
                if (double.TryParse(str, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var numValue))
                {
                    if (numValue < min || numValue > max)
                        errors[field] = $"Field '{field}' value {numValue} is outside range [{min}, {max}]";
                }
                else
                {
                    errors[field] = $"Field '{field}' value cannot be converted to a number";
                }
            }
        }

        if (errors.Count > 0)
        {
            return Task.FromResult<IGenericResult<ValidationRuleResult>>(
                GenericResult<ValidationRuleResult>.Success(
                    ValidationRuleResult.Failure("Range validation failed", errors)));
        }

        return Task.FromResult<IGenericResult<ValidationRuleResult>>(
            GenericResult<ValidationRuleResult>.Success(ValidationRuleResult.Success()));
    }
}
