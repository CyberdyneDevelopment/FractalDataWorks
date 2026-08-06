using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Data.Lineage.EdgeTypes;

/// <summary>
/// A pipeline within a step depends on another pipeline completing before it can start.
/// Used for: Pipeline → DependsOn → Pipeline (from pipe.StepPipelinePrerequisite).
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(LineageEdgeTypes), "DependsOn")]
public sealed class DependsOnEdgeType : LineageEdgeTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="DependsOnEdgeType"/> class.</summary>
    public DependsOnEdgeType() : base(9, "DependsOn") { }
}
