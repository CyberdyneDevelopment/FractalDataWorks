using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;

namespace Fdw.Services.Data.Execution;

/// <summary>
/// Runs the graph of a Graph data set for a read and answers its rows (FDW-5291).
/// </summary>
/// <remarks>
/// Why this is declared here and implemented elsewhere: the engine that runs a graph is above the data services, so the Graph kind, which is a
/// module-init type with no providers, reaches it through the execution context. The run keeps no run record. It reads its sources through the
/// gateway it is handed, so every source, a nested data set included, is read for the caller whose read this is, with that caller's row rules.
/// A run that cannot finish answers a named failure and never the rows read so far.
/// </remarks>
public interface IGraphDataSetRunner
{
    /// <summary>Runs the graph of a data set and collects the rows of its fields out.</summary>
    /// <param name="gateway">The gateway the graph's sources are read through.</param>
    /// <param name="dataSet">The Graph data set whose graph is run.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The rows, or the named failure that says why the read cannot be answered.</returns>
    Task<IGenericResult<IReadOnlyList<IDictionary<string, object?>>>> Read(
        IDataGateway gateway, IGraphDataSetConfiguration dataSet, CancellationToken cancellationToken = default);
}
