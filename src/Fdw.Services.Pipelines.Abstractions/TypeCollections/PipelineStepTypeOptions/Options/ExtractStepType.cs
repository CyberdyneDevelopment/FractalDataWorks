using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Pipelines.Abstractions.TypeCollections.PipelineStepTypeOptions.Options;

/// <summary>
/// Pipeline step type for data extraction from a source system.
/// </summary>
/// <remarks>
/// Extract steps read data from a configured source and pass it downstream in the pipeline.
/// They require source configuration specifying the data origin (connection, query, file path, etc.).
/// </remarks>
[TypeOption(typeof(PipelineStepTypes), "Extract")]
[ExcludeFromCodeCoverage]
public sealed class ExtractStepType : PipelineStepTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ExtractStepType"/> class.
    /// </summary>
    public ExtractStepType()
        : base(
            id: 1,
            name: "Extract",
            requiresSourceConfig: true,
            requiresTransformConfig: false,
            requiresTargetConfig: false,
            requiresValidationConfig: false,
            requiresNotificationConfig: false,
            requiresBranchCondition: false)
    {
    }
}
