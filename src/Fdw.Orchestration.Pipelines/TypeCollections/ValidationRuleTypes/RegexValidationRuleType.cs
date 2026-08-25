using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Orchestration.Pipelines.Abstractions.TypeCollections.ValidationRuleTypeOptions;
using Fdw.Orchestration.Pipelines.Results;
using Fdw.Results;
using ValidationRuleTypesCollection = Fdw.Orchestration.Pipelines.Abstractions.TypeCollections.ValidationRuleTypeOptions.ValidationRuleTypes;

namespace Fdw.Orchestration.Pipelines.TypeCollections.ValidationRuleTypes;

/// <summary>
/// Validation rule that checks if string values match a regular expression pattern.
/// </summary>
[TypeOption(typeof(ValidationRuleTypesCollection), "Regex", RestrictToCurrentCompilation = true)]
public sealed class RegexValidationRuleType : ValidationRuleTypeBase
{
    private static readonly IReadOnlyList<string> ParameterNames = new[] { "Pattern" };

    /// <summary>
    /// Initializes a new instance of the <see cref="RegexValidationRuleType"/> class.
    /// </summary>
    public RegexValidationRuleType()
        : base(
            id: 3,
            name: "Regex",
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

        if (!parameters.TryGetValue("Pattern", out var patternObj) || patternObj is not string pattern)
        {
            return Task.FromResult<IGenericResult<ValidationRuleResult>>(
                GenericResult<ValidationRuleResult>.Failure(PipelineResultCodes.ByName("RegexPatternRequired")));
        }

        Regex regex;
        try
        {
            regex = new Regex(pattern, RegexOptions.Compiled, TimeSpan.FromSeconds(1));
        }
        catch (ArgumentException ex)
        {
            return Task.FromResult<IGenericResult<ValidationRuleResult>>(
                GenericResult<ValidationRuleResult>.Failure(
                    PipelineResultCodes.ByName("InvalidRegexPattern"),
                    ResultDetails.Create("ErrorMessage", ex.Message)));
        }

        foreach (var field in fields)
        {
            if (record.TryGetValue(field, out var value) && value != null)
            {
                var stringValue = value.ToString() ?? string.Empty;
                if (!regex.IsMatch(stringValue))
                {
                    errors[field] = $"Field '{field}' value does not match pattern '{pattern}'";
                }
            }
        }

        if (errors.Count > 0)
        {
            return Task.FromResult<IGenericResult<ValidationRuleResult>>(
                GenericResult<ValidationRuleResult>.Success(
                    ValidationRuleResult.Failure("Regex validation failed", errors)));
        }

        return Task.FromResult<IGenericResult<ValidationRuleResult>>(
            GenericResult<ValidationRuleResult>.Success(ValidationRuleResult.Success()));
    }
}
