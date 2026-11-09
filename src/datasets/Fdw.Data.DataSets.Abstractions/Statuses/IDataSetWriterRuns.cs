using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Data.DataSets.Abstractions;

/// <summary>
/// The pipelines that write each data set, each with its latest run (FDW-4488).
/// </summary>
/// <remarks>
/// Why this contract is on the data set side: a data set's status is the data set domain's, but who writes it and how the last run went are the pipelines' to say. The pipelines
/// implement this and the data set domain asks it, so the data set domain does not depend on the pipelines.
/// </remarks>
public interface IDataSetWriterRuns
{
    /// <summary>Reads, for every data set a visible pipeline writes, its writers and the latest run of each.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// The data set's name (compared ignoring case) to its writers. A data set no visible pipeline writes is absent. The read costs a fixed number of statements however many
    /// pipelines there are, plus one for each chunk of pipelines whose latest runs are read.
    /// </returns>
    Task<IGenericResult<IReadOnlyDictionary<string, IReadOnlyList<DataSetWriterRun>>>> ByDataSet(CancellationToken cancellationToken = default);
}
