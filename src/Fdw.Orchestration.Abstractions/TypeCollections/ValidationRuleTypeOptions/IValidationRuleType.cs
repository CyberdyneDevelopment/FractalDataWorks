using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections;
using Fdw.Results;

namespace Fdw.Orchestration.Pipelines.Abstractions.TypeCollections.ValidationRuleTypeOptions;

/// <summary>
/// Interface for validation rule type TypeOptions.
/// </summary>
/// <remarks>
/// Validation rule types define different kinds of data validation:
/// required fields, range checks, regex patterns, referential integrity, etc.
/// </remarks>
public interface IValidationRuleType : ITypeOption<int, ValidationRuleTypeBase>
{
    /// <summary>
    /// Gets whether this rule type requires field specifications.
    /// </summary>
    bool RequiresFields { get; }

    /// <summary>
    /// Gets whether this rule type supports validation across multiple fields.
    /// </summary>
    bool SupportsMultipleFields { get; }

    /// <summary>
    /// Gets whether this rule type requires additional parameters.
    /// </summary>
    bool RequiresParameters { get; }

    /// <summary>
    /// Gets the names of required parameters for this rule type.
    /// </summary>
    IReadOnlyList<string> RequiredParameterNames { get; }

    /// <summary>
    /// Validates a record against this rule type.
    /// </summary>
    /// <param name="record">The record to validate.</param>
    /// <param name="fields">The fields this rule applies to.</param>
    /// <param name="parameters">Rule-specific parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing validation outcome.</returns>
    Task<IGenericResult<ValidationRuleResult>> Validate(
        IReadOnlyDictionary<string, object?> record,
        IReadOnlyList<string> fields,
        IReadOnlyDictionary<string, object?> parameters,
        CancellationToken cancellationToken = default);
}
