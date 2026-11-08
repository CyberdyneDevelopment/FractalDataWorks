using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Fdw.Collections.Attributes;

namespace Fdw.Data.DataSets.Abstractions;

/// <summary>The latest run of a pipeline that writes the data set is executing: it has taken its turn and is not finished.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DataSetStatuses), "Running", RestrictToCurrentCompilation = true)]
public sealed class RunningDataSetStatus : DataSetStatusBase
{
    /// <summary>Initializes a new instance of the <see cref="RunningDataSetStatus"/> class.</summary>
    public RunningDataSetStatus()
        : base("Running", 700)
    {
    }

    /// <inheritdoc/>
    public override bool Applies(DataSetStatusInput input)
        => input.Writers.Any(writer => writer.LatestRun is { } run && run.State.IsExecuting);
}
