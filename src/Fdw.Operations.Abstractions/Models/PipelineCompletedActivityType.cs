using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Operations.Clients.Models;

/// <summary>A pipeline execution completed successfully.</summary>
[TypeOption(typeof(ActivityTypes), "PipelineCompleted")]
[ExcludeFromCodeCoverage]
public sealed class PipelineCompletedActivityType : ActivityTypeBase
{
    /// <summary>Initializes a new instance of <see cref="PipelineCompletedActivityType"/>.</summary>
    public PipelineCompletedActivityType() : base(2, "PipelineCompleted") { }
}
