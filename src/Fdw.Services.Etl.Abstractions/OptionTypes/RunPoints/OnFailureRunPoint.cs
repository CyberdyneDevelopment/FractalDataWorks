using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>The notification fires once, when the run fails.</summary>
[TypeOption(typeof(RunPoints), "OnFailure")]
public sealed class OnFailureRunPoint : RunPointBase
{
    /// <summary>Initializes a new instance of the <see cref="OnFailureRunPoint"/> class.</summary>
    public OnFailureRunPoint() : base("OnFailure", "When the run fails", "Once, when the run fails after the pre-run check; its {{Rows*}} values are the counters at the failure, zero for a Streaming pipeline", firesAfterStage: false, firesAfterLoad: false, firesOnFailure: true)
    {
    }
}
