using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Data.DataSets.Abstractions;

/// <summary>
/// The status of each data set in one request, read once (FDW-4488).
/// </summary>
/// <remarks>
/// The writers, their latest runs and the window are read when the view is made, so each data set's status is then worked out in memory: a list costs the same however many
/// data sets it holds.
/// </remarks>
public interface IDataSetStatusView
{
    /// <summary>Works out one data set's status from what was read.</summary>
    /// <param name="dataSet">The data set.</param>
    /// <returns>
    /// The status; success with no value for a variation, which holds no data of its own, is written by no pipeline and has no status; for a Graph data set (FDW-5296), which no
    /// pipeline writes, the status of the pipelines that write the data sets it reads, or <c>NotApplicable</c> when it reads none; or the coded failure when no status option
    /// applies.
    /// </returns>
    IGenericResult<IDataSetStatus?> StatusOf(IDataSetImplementationConfiguration dataSet);

    /// <summary>Works out one data set's status the way <see cref="StatusOf"/> does, and follows a Graph data set through the data sets it nests (FDW-5299).</summary>
    /// <param name="dataSet">The data set.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>
    /// The same answer as <see cref="StatusOf"/> for every kind but a Graph. For a Graph data set, the status of the pipelines that write the data sets it reads, directly or through the Graph
    /// data sets and Variations it reads in turn (each followed once, so a cycle ends), or <c>NotApplicable</c> when it reaches none; or the coded failure of a read or of no status applying.
    /// </returns>
    /// <remarks>
    /// Why this is a separate member and not a change of <see cref="StatusOf"/>: what a Graph reads sits in its child rows, which a list does not read, and the nested data sets have to be read
    /// by name, so following a nest is a read. A page with no Graph data set reads nothing more. An implementer that knows nothing of nesting answers <see cref="StatusOf"/>.
    /// </remarks>
    Task<IGenericResult<IDataSetStatus?>> StatusOfNested(IDataSetImplementationConfiguration dataSet, CancellationToken cancellationToken = default) =>
        Task.FromResult(StatusOf(dataSet));
}
