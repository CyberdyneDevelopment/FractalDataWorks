using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections;
using Fdw.Results;

namespace Fdw.Orchestration.Pipelines.Abstractions.TypeCollections.ValidationRuleTypeOptions;

/// <summary>
/// Base class for validation rule type TypeOptions.
/// </summary>
/// <remarks>
/// Provides common functionality for validation rule types.
/// Derived classes implement specific validation logic.
/// </remarks>
public abstract class ValidationRuleTypeBase : TypeOptionBase<int, ValidationRuleTypeBase>, IValidationRuleType
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationRuleTypeBase"/> class.
    /// </summary>
    /// <param name="id">Unique numeric identifier.</param>
    /// <param name="name">Human-readable name.</param>
    /// <param name="requiresFields">Whether this rule type requires field specifications.</param>
    /// <param name="supportsMultipleFields">Whether this rule type supports multiple fields.</param>
    /// <param name="requiresParameters">Whether this rule type requires parameters.</param>
    /// <param name="requiredParameterNames">Names of required parameters.</param>
    protected ValidationRuleTypeBase(
        int id,
        string name,
        bool requiresFields,
        bool supportsMultipleFields,
        bool requiresParameters = false,
        IReadOnlyList<string>? requiredParameterNames = null)
        : base(id, name)
    {
        RequiresFields = requiresFields;
        SupportsMultipleFields = supportsMultipleFields;
        RequiresParameters = requiresParameters;
        RequiredParameterNames = requiredParameterNames ?? Array.Empty<string>();
    }

    /// <inheritdoc/>
    public bool RequiresFields { get; }

    /// <inheritdoc/>
    public bool SupportsMultipleFields { get; }

    /// <inheritdoc/>
    public bool RequiresParameters { get; }

    /// <inheritdoc/>
    public IReadOnlyList<string> RequiredParameterNames { get; }

    /// <inheritdoc/>
    public abstract Task<IGenericResult<ValidationResult>> Validate(
        IReadOnlyDictionary<string, object?> record,
        IReadOnlyList<string> fields,
        IReadOnlyDictionary<string, object?> parameters,
        CancellationToken cancellationToken = default);
}
