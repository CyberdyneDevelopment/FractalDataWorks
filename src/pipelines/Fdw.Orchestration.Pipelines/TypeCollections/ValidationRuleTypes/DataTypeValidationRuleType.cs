using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Orchestration.Pipelines.Abstractions.TypeCollections.ValidationRuleTypeOptions;
using Fdw.Orchestration.Pipelines.Results;
using Fdw.Orchestration.Pipelines.TypeCollections.ValidationData;
using Fdw.Results;
using ValidationRuleTypesCollection = Fdw.Orchestration.Pipelines.Abstractions.TypeCollections.ValidationRuleTypeOptions.ValidationRuleTypes;

namespace Fdw.Orchestration.Pipelines.TypeCollections.ValidationRuleTypes;

/// <summary>
/// Validation rule that checks if values can be converted to a specified data type.
/// </summary>
[TypeOption(typeof(ValidationRuleTypesCollection), "DataType", RestrictToCurrentCompilation = true)]
public sealed class DataTypeValidationRuleType : ValidationRuleTypeBase
{
    private static readonly IReadOnlyList<string> ParameterNames = new[] { "DataType" };

    /// <summary>
    /// Initializes a new instance of the <see cref="DataTypeValidationRuleType"/> class.
    /// </summary>
    public DataTypeValidationRuleType()
        : base(
            name: "DataType",
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

        if (!parameters.TryGetValue("DataType", out var dataTypeObj) || dataTypeObj is not string dataType)
        {
            return Task.FromResult<IGenericResult<ValidationRuleResult>>(
                GenericResult<ValidationRuleResult>.Failure(PipelineResultCodes.ByName("DataTypeParameterRequired")));
        }

        foreach (var field in fields)
        {
            if (record.TryGetValue(field, out var value) && value != null)
            {
                var isValid = ValidateDataType(value, dataType);
                if (!isValid)
                {
                    errors[field] = $"Field '{field}' value cannot be converted to {dataType}";
                }
            }
        }

        if (errors.Count > 0)
        {
            return Task.FromResult<IGenericResult<ValidationRuleResult>>(
                GenericResult<ValidationRuleResult>.Success(
                    ValidationRuleResult.Failure("DataType validation failed", errors)));
        }

        return Task.FromResult<IGenericResult<ValidationRuleResult>>(
            GenericResult<ValidationRuleResult>.Success(ValidationRuleResult.Success()));
    }

    private static bool ValidateDataType(object value, string dataType)
    {
        var stringValue = value.ToString();
        if (stringValue == null)
            return false;

        return ValidationDataKinds.ByWord(dataType) is { } kind && kind.Accepts(stringValue);
    }
}
