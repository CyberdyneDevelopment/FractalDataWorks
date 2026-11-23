using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions;

/// <summary>A named output.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ComputeTableSources), "Output")]
public sealed class OutputComputeTableSource : ComputeTableSourceBase
{
    /// <summary>Initializes a new instance of the <see cref="OutputComputeTableSource"/> class.</summary>
    public OutputComputeTableSource()
        : base("Output", true, "a data set or output source states its name")
    {
    }
}
