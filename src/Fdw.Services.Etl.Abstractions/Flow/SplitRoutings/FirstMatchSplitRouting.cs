using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.Flow;

/// <summary>The row goes to the first output, in the order stated, whose filter it meets: SSIS Conditional Split.</summary>
[TypeOption(typeof(SplitRoutings), "FirstMatch")]
public sealed class FirstMatchSplitRouting : SplitRoutingBase
{
    /// <summary>Initializes a new instance of the <see cref="FirstMatchSplitRouting"/> class.</summary>
    public FirstMatchSplitRouting() : base("FirstMatch", "The row goes to the first output, in the order stated, whose filter it meets.", sendsToEveryMatch: false)
    {
    }
}
