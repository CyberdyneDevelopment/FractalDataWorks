using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Conventions;
using Fdw.Orchestration.Pipelines.Abstractions.TypeCollections.ValidationRuleTypeOptions;
using Fdw.Orchestration.Pipelines.Results;
using Fdw.Results;
using ValidationRuleTypesCollection = Fdw.Orchestration.Pipelines.Abstractions.TypeCollections.ValidationRuleTypeOptions.ValidationRuleTypes;

namespace Fdw.Orchestration.Pipelines.TypeCollections.ValidationRuleTypes;

/// <summary>
/// Validation rule that checks if values are in a predefined list of allowed values.
/// </summary>
[TypeOption(typeof(ValidationRuleTypesCollection), "InList", RestrictToCurrentCompilation = true)]
public sealed class InListValidationRuleType : ValidationRuleTypeBase
{
    private static readonly IReadOnlyList<string> ParameterNames = new[] { "AllowedValues" };

    /// <summary>
    /// Initializes a new instance of the <see cref="InListValidationRuleType"/> class.
    /// </summary>
    public InListValidationRuleType()
        : base(
            id: 6,
            name: "InList",
            requiresFields: true,
            supportsMultipleFields: true,
            requiresParameters: true,
            requiredParameterNames: ParameterNames)
    {
    }

    /// <inheritdoc/>
    [ConventionOverride(MaxCyclomaticComplexity = 20)]  // Validation logic — parameter parsing, type conversions, field validation
    public override Task<IGenericResult<ValidationResult>> Validate(
        IReadOnlyDictionary<string, object?> record,
        IReadOnlyList<string> fields,
        IReadOnlyDictionary<string, object?> parameters,
        CancellationToken cancellationToken = default)
    {
        var errors = new Dictionary<string, string>(StringComparer.Ordinal);

        if (!parameters.TryGetValue("AllowedValues", out var allowedObj))
        {
            return Task.FromResult<IGenericResult<ValidationResult>>(
                GenericResult<ValidationResult>.Failure(PipelineResultCodes.ByName("AllowedValuesRequired")));
        }

        var allowedValues = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (allowedObj is IEnumerable<string> stringList)
        {
            foreach (var v in stringList)
                allowedValues.Add(v);
        }
        else if (allowedObj is IEnumerable<object> objList)
        {
            foreach (var v in objList)
                allowedValues.Add(v?.ToString() ?? string.Empty);
        }
        else if (allowedObj is string csvList)
        {
            foreach (var v in csvList.Split(','))
                allowedValues.Add(v.Trim());
        }

        foreach (var field in fields)
        {
            if (record.TryGetValue(field, out var value) && value != null)
            {
                var stringValue = value.ToString() ?? string.Empty;
                if (!allowedValues.Contains(stringValue))
                {
                    errors[field] = $"Field '{field}' value '{stringValue}' is not in allowed values";
                }
            }
        }

        if (errors.Count > 0)
        {
            return Task.FromResult<IGenericResult<ValidationResult>>(
                GenericResult<ValidationResult>.Success(
                    ValidationResult.Failure("InList validation failed", errors)));
        }

        return Task.FromResult<IGenericResult<ValidationResult>>(
            GenericResult<ValidationResult>.Success(ValidationResult.Success()));
    }
}
