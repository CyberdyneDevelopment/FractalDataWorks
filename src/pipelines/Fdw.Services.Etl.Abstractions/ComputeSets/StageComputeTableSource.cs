using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions;

/// <summary>The stage's own rows, which have no name.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ComputeTableSources), "Stage")]
public sealed class StageComputeTableSource : ComputeTableSourceBase
{
    /// <summary>Initializes a new instance of the <see cref="StageComputeTableSource"/> class.</summary>
    public StageComputeTableSource()
        : base("Stage", false, "the stage's own rows have no name")
    {
    }
}
