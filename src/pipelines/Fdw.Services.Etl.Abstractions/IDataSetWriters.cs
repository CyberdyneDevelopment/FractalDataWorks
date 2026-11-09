using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Etl.Abstractions;

/// <summary>
/// Which pipelines write each data set (FDW-4488).
/// </summary>
/// <remarks>
/// A pipeline writes a data set when one of its components, of a type with no outputs, names it. The list is derived from the pipelines themselves: a change to a
/// pipeline changes who writes what.
/// </remarks>
public interface IDataSetWriters
{
    /// <summary>
    /// Reads, for every data set a visible pipeline writes, the pipelines that write it.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// The data set's name (compared ignoring case) to its writers. A data set no visible pipeline writes is absent. A writer that names its data set by a placeholder
    /// (<c>{{Parameter}}</c>) is left out: its data set is only known for a run. The read costs a fixed number of statements however many pipelines there are.
    /// </returns>
    Task<IGenericResult<IReadOnlyDictionary<string, IReadOnlyList<DataSetWriter>>>> ByDataSet(CancellationToken cancellationToken = default);
}
