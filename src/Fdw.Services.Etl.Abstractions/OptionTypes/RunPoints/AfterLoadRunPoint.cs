using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>The notification fires once, after the run's load has committed.</summary>
[TypeOption(typeof(RunPoints), "AfterLoad")]
public sealed class AfterLoadRunPoint : RunPointBase
{
    /// <summary>Initializes a new instance of the <see cref="AfterLoadRunPoint"/> class.</summary>
    public AfterLoadRunPoint() : base("AfterLoad", "After the load", "Once, after the load commits and the watermark is stored; for a Streaming pipeline, when the stream ends", firesAfterStage: false, firesAfterLoad: true, firesOnFailure: false)
    {
    }
}
