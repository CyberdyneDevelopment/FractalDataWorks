using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Pipelines.Clients.Models;

/// <summary>Pipeline is active and may be executed.</summary>
[TypeOption(typeof(PipelineStatuses), "Active")]
[ExcludeFromCodeCoverage]
public sealed class ActivePipelineStatus : PipelineStatusBase
{
    /// <summary>Initializes a new instance of <see cref="ActivePipelineStatus"/>.</summary>
    public ActivePipelineStatus() : base(1, "Active") { }
}
