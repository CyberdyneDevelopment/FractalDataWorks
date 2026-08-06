using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Pipelines.Clients.Models;

/// <summary>Pipeline is being authored and is not yet active.</summary>
[TypeOption(typeof(PipelineStatuses), "Draft")]
[ExcludeFromCodeCoverage]
public sealed class DraftPipelineStatus : PipelineStatusBase
{
    /// <summary>Initializes a new instance of <see cref="DraftPipelineStatus"/>.</summary>
    public DraftPipelineStatus() : base(0, "Draft") { }
}
