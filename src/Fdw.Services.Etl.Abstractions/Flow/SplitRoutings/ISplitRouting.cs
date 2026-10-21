using Fdw.Collections;

namespace Fdw.Services.Etl.Abstractions.Flow;

/// <summary>
/// How a split sends a row that meets more than one of its filters (FDW-1386): to the first, or to every one.
/// </summary>
public interface ISplitRouting : ITypeOption<int, ISplitRouting>
{
    /// <summary>Gets whether a row goes to every output whose filter it meets rather than to the first only.</summary>
    bool SendsToEveryMatch { get; }
}
