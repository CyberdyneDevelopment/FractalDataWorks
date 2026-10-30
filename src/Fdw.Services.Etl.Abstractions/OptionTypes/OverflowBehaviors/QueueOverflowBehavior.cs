using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>The run waits for its turn and starts when one of the pipeline's active runs ends.</summary>
[TypeOption(typeof(OverflowBehaviors), "Queue")]
public sealed class QueueOverflowBehavior : OverflowBehaviorBase
{
    /// <summary>Initializes a new instance of the <see cref="QueueOverflowBehavior"/> class.</summary>
    public QueueOverflowBehavior() : base("Queue", "The run waits for its turn and starts when one of the pipeline's active runs ends.", refusesWhenFull: false)
    {
    }
}
