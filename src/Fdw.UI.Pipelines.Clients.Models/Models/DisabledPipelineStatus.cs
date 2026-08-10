using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Pipelines.Clients.Models;

/// <summary>Pipeline is disabled and will not execute.</summary>
[TypeOption(typeof(PipelineStatuses), "Disabled")]
[ExcludeFromCodeCoverage]
public sealed class DisabledPipelineStatus : PipelineStatusBase
{
    /// <summary>Initializes a new instance of <see cref="DisabledPipelineStatus"/>.</summary>
    public DisabledPipelineStatus() : base(2, "Disabled") { }
}
