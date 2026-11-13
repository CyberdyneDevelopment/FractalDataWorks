using System.Collections.Generic;

namespace Fdw.Services.Data;

/// <summary>What a walk through the data sets a data set reads found (FDW-5290): a cycle, or how deep the nesting goes.</summary>
/// <param name="Cycle">The data set names around the cycle, the first repeated at the end (<c>A, B, C, A</c>); empty when there is none.</param>
/// <param name="Depth">The most data sets in a chain below the start (a start that reads only a data set that reads nothing is 1; one that reads none is 0); 0 when there is a cycle.</param>
public sealed record GraphDataSetCycleResult(IReadOnlyList<string> Cycle, int Depth)
{
    /// <summary>Gets a value indicating whether the start reads itself through data sets.</summary>
    public bool HasCycle => Cycle.Count > 0;
}
