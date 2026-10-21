using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.Flow;

/// <summary>The row goes to every output whose filter it meets: a filtered multicast.</summary>
[TypeOption(typeof(SplitRoutings), "AllMatches")]
public sealed class AllMatchesSplitRouting : SplitRoutingBase
{
    /// <summary>Initializes a new instance of the <see cref="AllMatchesSplitRouting"/> class.</summary>
    public AllMatchesSplitRouting() : base("AllMatches", "The row goes to every output whose filter it meets.", sendsToEveryMatch: true)
    {
    }
}
