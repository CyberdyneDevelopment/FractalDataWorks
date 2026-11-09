using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Fdw.Collections.Attributes;

namespace Fdw.Data.DataSets.Abstractions;

/// <summary>No pipeline writes the data set.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DataSetStatuses), "Unwritten", RestrictToCurrentCompilation = true)]
public sealed class UnwrittenDataSetStatus : DataSetStatusBase
{
    /// <summary>Initializes a new instance of the <see cref="UnwrittenDataSetStatus"/> class.</summary>
    public UnwrittenDataSetStatus()
        : base("Unwritten", 900)
    {
    }

    /// <inheritdoc/>
    public override bool Applies(DataSetStatusInput input)
        => input.Writers.Count == 0;
}
