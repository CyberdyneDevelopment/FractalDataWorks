using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Pipelines.Abstractions.TypeCollections.PipelineStepTypeOptions.Options;

/// <summary>
/// Pipeline step type for loading data into a target system.
/// </summary>
/// <remarks>
/// Load steps write data to a configured target destination such as a database, file, or API.
/// They require target configuration specifying the data destination and write behavior.
/// </remarks>
[TypeOption(typeof(PipelineStepTypes), "Load")]
[ExcludeFromCodeCoverage]
public sealed class LoadStepType : PipelineStepTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LoadStepType"/> class.
    /// </summary>
    public LoadStepType()
        : base(
            id: 3,
            name: "Load",
            requiresSourceConfig: false,
            requiresTransformConfig: false,
            requiresTargetConfig: true,
            requiresValidationConfig: false,
            requiresNotificationConfig: false,
            requiresBranchCondition: false)
    {
    }
}
