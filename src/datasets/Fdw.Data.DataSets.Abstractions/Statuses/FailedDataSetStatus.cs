using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Fdw.Collections.Attributes;

namespace Fdw.Data.DataSets.Abstractions;

/// <summary>The latest run of a pipeline that writes the data set failed.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DataSetStatuses), "Failed", RestrictToCurrentCompilation = true)]
public sealed class FailedDataSetStatus : DataSetStatusBase
{
    /// <summary>Initializes a new instance of the <see cref="FailedDataSetStatus"/> class.</summary>
    public FailedDataSetStatus()
        : base("Failed", 800)
    {
    }

    /// <inheritdoc/>
    public override bool Applies(DataSetStatusInput input)
        => input.Writers.Any(writer => writer.LatestRun is { } run && run.State.IsFailure);
}
