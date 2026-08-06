using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Data.Lineage.NodeTypes;

/// <summary>
/// An ETL project step node — an ordered unit within a Stage that contains one or more Pipelines.
/// Pipelines within a step may run in parallel subject to the MaxParallelPipelines policy.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(LineageNodeTypes), "Step")]
public sealed class StepNodeType : LineageNodeTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="StepNodeType"/> class.</summary>
    public StepNodeType() : base(8, "Step") { }
}
