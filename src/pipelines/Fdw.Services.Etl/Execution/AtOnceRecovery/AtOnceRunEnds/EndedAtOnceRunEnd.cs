using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Execution.AtOnceRecovery;

/// <summary>The run was carried out at once and was ended here, its row and its records closed.</summary>
[TypeOption(typeof(AtOnceRunEnds), "Ended")]
public sealed class EndedAtOnceRunEnd : AtOnceRunEndBase
{
    /// <summary>Initializes a new instance of the <see cref="EndedAtOnceRunEnd"/> class.</summary>
    public EndedAtOnceRunEnd()
        : base("Ended", "The run was carried out at once and was ended as its pipeline's half-done rule says: its row and its records are closed.", true, true)
    {
    }
}
