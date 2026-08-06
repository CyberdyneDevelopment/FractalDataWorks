using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Operations.Clients.Models;

/// <summary>A pipeline execution failed.</summary>
[TypeOption(typeof(ActivityTypes), "PipelineFailed")]
[ExcludeFromCodeCoverage]
public sealed class PipelineFailedActivityType : ActivityTypeBase
{
    /// <summary>Initializes a new instance of <see cref="PipelineFailedActivityType"/>.</summary>
    public PipelineFailedActivityType() : base(3, "PipelineFailed") { }
}
