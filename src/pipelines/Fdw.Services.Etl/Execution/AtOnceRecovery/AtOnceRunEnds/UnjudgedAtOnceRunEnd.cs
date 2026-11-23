using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Execution.AtOnceRecovery;

/// <summary>The run could not be judged and is left open.</summary>
[TypeOption(typeof(AtOnceRunEnds), "Unjudged")]
public sealed class UnjudgedAtOnceRunEnd : AtOnceRunEndBase
{
    /// <summary>Initializes a new instance of the <see cref="UnjudgedAtOnceRunEnd"/> class.</summary>
    public UnjudgedAtOnceRunEnd()
        : base("Unjudged", "The run could not be judged (its record, its pipeline or its rule could not be read, or its record could not be closed): it is left open, and the reason is logged.", false, false)
    {
    }
}
