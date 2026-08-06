using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Operations.Clients.Models;

/// <summary>A pipeline execution started.</summary>
[TypeOption(typeof(ActivityTypes), "PipelineStarted")]
[ExcludeFromCodeCoverage]
public sealed class PipelineStartedActivityType : ActivityTypeBase
{
    /// <summary>Initializes a new instance of <see cref="PipelineStartedActivityType"/>.</summary>
    public PipelineStartedActivityType() : base(1, "PipelineStarted") { }
}
