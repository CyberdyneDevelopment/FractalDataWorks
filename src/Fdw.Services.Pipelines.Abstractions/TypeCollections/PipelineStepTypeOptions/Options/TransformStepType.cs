using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Pipelines.Abstractions.TypeCollections.PipelineStepTypeOptions.Options;

/// <summary>
/// Pipeline step type for data transformation.
/// </summary>
/// <remarks>
/// Transform steps apply configured transformation logic to data passing through the pipeline.
/// They require transform configuration specifying the mappings, calculations, or other
/// transformations to apply to the input data.
/// </remarks>
[TypeOption(typeof(PipelineStepTypes), "Transform")]
[ExcludeFromCodeCoverage]
public sealed class TransformStepType : PipelineStepTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TransformStepType"/> class.
    /// </summary>
    public TransformStepType()
        : base(
            id: 2,
            name: "Transform",
            requiresSourceConfig: false,
            requiresTransformConfig: true,
            requiresTargetConfig: false,
            requiresValidationConfig: false,
            requiresNotificationConfig: false,
            requiresBranchCondition: false)
    {
    }
}
