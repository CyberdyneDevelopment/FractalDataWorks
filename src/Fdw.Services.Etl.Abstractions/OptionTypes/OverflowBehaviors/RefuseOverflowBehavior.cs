using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>The run ends as failed and names the pipeline and its limit.</summary>
[TypeOption(typeof(OverflowBehaviors), "Refuse")]
public sealed class RefuseOverflowBehavior : OverflowBehaviorBase
{
    /// <summary>Initializes a new instance of the <see cref="RefuseOverflowBehavior"/> class.</summary>
    public RefuseOverflowBehavior() : base("Refuse", "The run ends as failed and names the pipeline and its limit.", refusesWhenFull: true)
    {
    }
}
