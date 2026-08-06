using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Pipelines.Abstractions.TypeCollections.PipelineStepTypeOptions.Options;

/// <summary>
/// Pipeline step type for conditional branching.
/// </summary>
/// <remarks>
/// Branch steps evaluate a condition and direct pipeline execution to one of multiple downstream
/// paths based on the result. They require a branch condition specifying the evaluation expression
/// and the routing logic for each outcome.
/// </remarks>
[TypeOption(typeof(PipelineStepTypes), "Branch")]
[ExcludeFromCodeCoverage]
public sealed class BranchStepType : PipelineStepTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BranchStepType"/> class.
    /// </summary>
    public BranchStepType()
        : base(
            id: 6,
            name: "Branch",
            requiresSourceConfig: false,
            requiresTransformConfig: false,
            requiresTargetConfig: false,
            requiresValidationConfig: false,
            requiresNotificationConfig: false,
            requiresBranchCondition: true)
    {
    }
}
