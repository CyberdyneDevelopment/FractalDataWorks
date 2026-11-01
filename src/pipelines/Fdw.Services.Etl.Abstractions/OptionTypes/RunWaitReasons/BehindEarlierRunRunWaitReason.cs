using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>A run that arrived earlier writes a table this run writes, is of the same pipeline, or must run alone, and goes first.</summary>
[TypeOption(typeof(RunWaitReasons), "BehindEarlierRun")]
public sealed class BehindEarlierRunRunWaitReason : RunWaitReasonBase
{
    /// <summary>Initializes a new instance of the <see cref="BehindEarlierRunRunWaitReason"/> class.</summary>
    public BehindEarlierRunRunWaitReason() : base("BehindEarlierRun", "A run that arrived earlier writes a table this run writes, is of the same pipeline, or must run alone, and goes first.")
    {
    }
}
