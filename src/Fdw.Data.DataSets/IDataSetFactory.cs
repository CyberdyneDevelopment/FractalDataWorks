using Fdw.Data.Abstractions;
using Fdw.Results;

namespace Fdw.Data.DataSets.Abstractions;

/// <summary>
/// Builds a live <see cref="IDataSet"/> runtime from a <see cref="DataSetConfiguration"/> record.
/// </summary>
/// <remarks>
/// <para>
/// The factory uses the Sources already composed onto the <see cref="DataSetConfiguration"/> by
/// <see cref="IDataSetConfigurationProvider"/> to construct <see cref="IDataSetSource"/> and
/// <see cref="IDataSetJoin"/> instances, infers the <see cref="IDataSetCompositionType"/> from the
/// source/join count, and assembles the final <see cref="IDataSet"/> graph.
/// </para>
/// <para>
/// This is a synchronous operation — all required configuration is already resolved by the time
/// the factory is called. Inject <see cref="IDataSetConfigurationProvider"/> to obtain a
/// <see cref="DataSetConfiguration"/> before passing it to this factory.
/// </para>
/// </remarks>
public interface IDataSetFactory
{
    /// <summary>
    /// Creates a live <see cref="IDataSet"/> runtime from the supplied configuration record.
    /// </summary>
    /// <param name="config">The DataSet configuration record to materialize as a runtime graph.</param>
    /// <returns>
    /// A success result containing the constructed <see cref="IDataSet"/>, or a failure result
    /// with a structured error message if source resolution or join construction fails.
    /// </returns>
    IGenericResult<IDataSet> Create(DataSetConfiguration config);
}
