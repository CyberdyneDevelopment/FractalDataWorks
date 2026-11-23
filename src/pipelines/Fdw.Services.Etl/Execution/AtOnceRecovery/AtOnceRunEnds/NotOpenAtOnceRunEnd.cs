using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Execution.AtOnceRecovery;

/// <summary>The run is not an open run carried out at once, so nothing is closed here.</summary>
[TypeOption(typeof(AtOnceRunEnds), "NotOpenAtOnce")]
public sealed class NotOpenAtOnceRunEnd : AtOnceRunEndBase
{
    /// <summary>Initializes a new instance of the <see cref="NotOpenAtOnceRunEnd"/> class.</summary>
    public NotOpenAtOnceRunEnd()
        : base("NotOpenAtOnce", "The run is not an open run carried out at once (a queued run, a run already ended, or one with no record): nothing was closed and its own path decides.", true, false)
    {
    }
}
