using Fdw.Collections;

namespace Fdw.Services.Pipelines.Abstractions.TypeCollections.PipelineStepTypeOptions;

/// <summary>
/// Represents a pipeline step type that defines the role and configuration requirements of a step.
/// </summary>
public interface IPipelineStepType : ITypeOption<int, PipelineStepTypeBase>
{
    /// <summary>
    /// Gets a value indicating whether this step type requires source configuration.
    /// </summary>
    bool RequiresSourceConfig { get; }

    /// <summary>
    /// Gets a value indicating whether this step type requires transform configuration.
    /// </summary>
    bool RequiresTransformConfig { get; }

    /// <summary>
    /// Gets a value indicating whether this step type requires target configuration.
    /// </summary>
    bool RequiresTargetConfig { get; }

    /// <summary>
    /// Gets a value indicating whether this step type requires validation configuration.
    /// </summary>
    bool RequiresValidationConfig { get; }

    /// <summary>
    /// Gets a value indicating whether this step type requires notification configuration.
    /// </summary>
    bool RequiresNotificationConfig { get; }

    /// <summary>
    /// Gets a value indicating whether this step type requires a branch condition.
    /// </summary>
    bool RequiresBranchCondition { get; }
}
