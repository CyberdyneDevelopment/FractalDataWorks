using Fdw.Collections;

namespace Fdw.Services.Quality.Abstractions.TypeCollections.QualityRuleTypeOptions;

/// <summary>
/// Base class for quality rule types using the CRTP pattern.
/// </summary>
public abstract class QualityRuleTypeBase : TypeOptionBase<int, QualityRuleTypeBase>, IQualityRuleType
{
    /// <summary>
    /// Initializes a new instance of the <see cref="QualityRuleTypeBase"/> class.
    /// </summary>
    /// <param name="id">The unique identifier.</param>
    /// <param name="name">The unique name.</param>
    /// <param name="requiresField">Whether this rule requires a specific field.</param>
    /// <param name="supportsMultipleFields">Whether this rule supports multiple fields.</param>
    /// <param name="requiresParameters">Whether this rule requires additional parameters.</param>
    /// <param name="description">The human-readable description.</param>
    protected QualityRuleTypeBase(
        int id,
        string name,
        bool requiresField,
        bool supportsMultipleFields,
        bool requiresParameters,
        string description)
        : base(id, name, $"TypeOptions:{name}", name, description, null)
    {
        RequiresField = requiresField;
        SupportsMultipleFields = supportsMultipleFields;
        RequiresParameters = requiresParameters;
    }

    /// <inheritdoc/>
    public bool RequiresField { get; }

    /// <inheritdoc/>
    public bool SupportsMultipleFields { get; }

    /// <inheritdoc/>
    public bool RequiresParameters { get; }
}
