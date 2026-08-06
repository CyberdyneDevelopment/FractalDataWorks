using Fdw.Collections;

namespace Fdw.Services.Pipelines.Abstractions.TypeCollections.PipelineStepTypeOptions;

/// <summary>
/// Base class for pipeline step types using the CRTP pattern.
/// </summary>
/// <remarks>
/// Pipeline step types define the role and configuration requirements for each step in a pipeline.
/// Each derived type enables one specific configuration requirement via a dedicated boolean flag,
/// while all other flags default to <c>false</c>.
/// </remarks>
public abstract class PipelineStepTypeBase : TypeOptionBase<int, PipelineStepTypeBase>, IPipelineStepType
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PipelineStepTypeBase"/> class.
    /// </summary>
    /// <param name="id">The unique identifier.</param>
    /// <param name="name">The unique name.</param>
    /// <param name="requiresSourceConfig">Whether this step type requires source configuration.</param>
    /// <param name="requiresTransformConfig">Whether this step type requires transform configuration.</param>
    /// <param name="requiresTargetConfig">Whether this step type requires target configuration.</param>
    /// <param name="requiresValidationConfig">Whether this step type requires validation configuration.</param>
    /// <param name="requiresNotificationConfig">Whether this step type requires notification configuration.</param>
    /// <param name="requiresBranchCondition">Whether this step type requires a branch condition.</param>
    protected PipelineStepTypeBase(
        int id,
        string name,
        bool requiresSourceConfig,
        bool requiresTransformConfig,
        bool requiresTargetConfig,
        bool requiresValidationConfig,
        bool requiresNotificationConfig,
        bool requiresBranchCondition)
        : base(id, name)
    {
        RequiresSourceConfig = requiresSourceConfig;
        RequiresTransformConfig = requiresTransformConfig;
        RequiresTargetConfig = requiresTargetConfig;
        RequiresValidationConfig = requiresValidationConfig;
        RequiresNotificationConfig = requiresNotificationConfig;
        RequiresBranchCondition = requiresBranchCondition;
    }

    /// <inheritdoc/>
    public bool RequiresSourceConfig { get; }

    /// <inheritdoc/>
    public bool RequiresTransformConfig { get; }

    /// <inheritdoc/>
    public bool RequiresTargetConfig { get; }

    /// <inheritdoc/>
    public bool RequiresValidationConfig { get; }

    /// <inheritdoc/>
    public bool RequiresNotificationConfig { get; }

    /// <inheritdoc/>
    public bool RequiresBranchCondition { get; }
}
