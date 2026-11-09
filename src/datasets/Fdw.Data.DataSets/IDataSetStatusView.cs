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
    /// The status; success with no value for a variation, which holds no data of its own, is written by no pipeline and has no status; or the coded failure when no status option
    /// applies.
    /// </returns>
    IGenericResult<IDataSetStatus?> StatusOf(IDataSetImplementationConfiguration dataSet);
}
