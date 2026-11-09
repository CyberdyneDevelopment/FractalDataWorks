using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Fdw.Collections.Attributes;

namespace Fdw.Data.DataSets.Abstractions;

/// <summary>A pipeline that writes the data set completed its latest run within the window.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DataSetStatuses), "Current", RestrictToCurrentCompilation = true)]
public sealed class CurrentDataSetStatus : DataSetStatusBase
{
    /// <summary>Initializes a new instance of the <see cref="CurrentDataSetStatus"/> class.</summary>
    public CurrentDataSetStatus()
        : base("Current", 500)
    {
    }

    /// <inheritdoc/>
    public override bool Applies(DataSetStatusInput input)
        => input.Writers.Any(writer => writer.LatestRun is { CompletedAt: { } completedAt } run && run.State.IsSuccess && input.Now - completedAt <= input.Window);
}
