using System;

namespace Fdw.Services.Etl.Flow;

/// <summary>
/// A component told how many rows were rejected upstream of it in its run (FDW-4956): an absent-key sweep that would flag a rejected row's key as gone
/// reads it before it runs.
/// </summary>
internal interface IFlowRejectionsBound
{
    /// <summary>Gives the component the reader of the rows rejected upstream of it; it is read once the component's input has ended.</summary>
    /// <param name="rejectedUpstream">Answers the rows rejected by every component with a path of links to this one.</param>
    void BindRejections(Func<long> rejectedUpstream);
}
