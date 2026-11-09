using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Fdw.Collections.Attributes;

namespace Fdw.Data.DataSets.Abstractions;

/// <summary>A pipeline writes the data set, and none has completed a run within the window: nothing has fed it lately.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DataSetStatuses), "Idle", RestrictToCurrentCompilation = true)]
public sealed class IdleDataSetStatus : DataSetStatusBase
{
    /// <summary>Initializes a new instance of the <see cref="IdleDataSetStatus"/> class.</summary>
    public IdleDataSetStatus()
        : base("Idle", 100)
    {
    }

    /// <inheritdoc/>
    public override bool Applies(DataSetStatusInput input)
        => input.Writers.Count > 0;
}
