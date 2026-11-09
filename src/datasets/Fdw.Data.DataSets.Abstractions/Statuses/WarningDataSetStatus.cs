using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Fdw.Collections.Attributes;

namespace Fdw.Data.DataSets.Abstractions;

/// <summary>The latest run of a pipeline that writes the data set was cancelled, timed out or interrupted.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DataSetStatuses), "Warning", RestrictToCurrentCompilation = true)]
public sealed class WarningDataSetStatus : DataSetStatusBase
{
    /// <summary>Initializes a new instance of the <see cref="WarningDataSetStatus"/> class.</summary>
    public WarningDataSetStatus()
        : base("Warning", 600)
    {
    }

    /// <inheritdoc/>
    public override bool Applies(DataSetStatusInput input)
        => input.Writers.Any(writer => writer.LatestRun is { } run && run.State.IsStopped);
}
