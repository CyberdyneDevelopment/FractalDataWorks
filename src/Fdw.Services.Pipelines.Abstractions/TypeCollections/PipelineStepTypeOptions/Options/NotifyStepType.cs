using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Pipelines.Abstractions.TypeCollections.PipelineStepTypeOptions.Options;

/// <summary>
/// Pipeline step type for sending notifications.
/// </summary>
/// <remarks>
/// Notify steps send alerts or status messages to configured recipients or systems.
/// They require notification configuration specifying the channel, recipients, message
/// template, and triggering conditions.
/// </remarks>
[TypeOption(typeof(PipelineStepTypes), "Notify")]
[ExcludeFromCodeCoverage]
public sealed class NotifyStepType : PipelineStepTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NotifyStepType"/> class.
    /// </summary>
    public NotifyStepType()
        : base(
            id: 5,
            name: "Notify",
            requiresSourceConfig: false,
            requiresTransformConfig: false,
            requiresTargetConfig: false,
            requiresValidationConfig: false,
            requiresNotificationConfig: true,
            requiresBranchCondition: false)
    {
    }
}
