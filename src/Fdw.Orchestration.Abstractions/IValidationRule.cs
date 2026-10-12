using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Orchestration.Pipelines.Abstractions.TypeCollections.ValidationRuleTypeOptions;
using Fdw.Orchestration.Abstractions.TypeCollections.ValidationSeverityOptions;
using Fdw.Results;

namespace Fdw.Orchestration.Pipelines.Abstractions;

/// <summary>
/// Defines a data validation rule.
/// </summary>
public interface IValidationRule
{
    /// <summary>
    /// Gets the unique identifier for this validation rule.
    /// </summary>
    string RuleId { get; }

    /// <summary>
    /// Gets the name of the validation rule.
    /// </summary>
    string RuleName { get; }

    /// <summary>
    /// Gets the type of validation rule.
    /// </summary>
    ValidationRuleTypeBase RuleType { get; }

    /// <summary>
    /// Gets the field(s) this rule applies to.
    /// </summary>
    IReadOnlyList<string> Fields { get; }

    /// <summary>
    /// Gets the rule parameters.
    /// </summary>
    IReadOnlyDictionary<string, object?> Parameters { get; }

    /// <summary>
    /// Gets the severity of this rule.
    /// </summary>
    ValidationSeverityBase Severity { get; }

    /// <summary>
    /// Gets the error message template for when validation fails.
    /// </summary>
    string ErrorMessageTemplate { get; }

    /// <summary>
    /// Validates a record against this rule.
    /// </summary>
    /// <param name="record">The record to validate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result indicating whether the record is valid.</returns>
    Task<IGenericResult<ValidationRuleResult>> Validate(
        IDictionary<string, object?> record,
        CancellationToken cancellationToken = default);
}