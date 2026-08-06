using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Pipelines.Abstractions.TypeCollections.PipelineStepTypeOptions.Options;

/// <summary>
/// Pipeline step type for data validation.
/// </summary>
/// <remarks>
/// Validate steps apply configured validation rules to pipeline data, ensuring it meets
/// quality and integrity requirements before proceeding. They require validation configuration
/// specifying the rules, thresholds, and failure behavior.
/// </remarks>
[TypeOption(typeof(PipelineStepTypes), "Validate")]
[ExcludeFromCodeCoverage]
public sealed class ValidateStepType : PipelineStepTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ValidateStepType"/> class.
    /// </summary>
    public ValidateStepType()
        : base(
            id: 4,
            name: "Validate",
            requiresSourceConfig: false,
            requiresTransformConfig: false,
            requiresTargetConfig: false,
            requiresValidationConfig: true,
            requiresNotificationConfig: false,
            requiresBranchCondition: false)
    {
    }
}
