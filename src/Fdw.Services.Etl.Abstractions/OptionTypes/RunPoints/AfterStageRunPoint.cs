using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>The notification fires once, after the final page of the run has passed the stage.</summary>
[TypeOption(typeof(RunPoints), "AfterStage")]
public sealed class AfterStageRunPoint : RunPointBase
{
    /// <summary>Initializes a new instance of the <see cref="AfterStageRunPoint"/> class.</summary>
    public AfterStageRunPoint() : base("AfterStage", "After the last page", "Once, after the last page has been transformed and loaded; for a Streaming pipeline, when the stream ends", firesAfterStage: true, firesAfterLoad: false, firesOnFailure: false)
    {
    }
}
