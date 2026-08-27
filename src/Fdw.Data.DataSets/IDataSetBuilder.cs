using Fdw.Data.Abstractions;
using Fdw.Results;
using System.Threading;
using System.Threading.Tasks;

namespace Fdw.Data.DataSets.Abstractions;

/// <summary>
/// Builds a live <see cref="IDataSet"/> runtime from a <see cref="DataSetConfiguration"/>.
/// </summary>
/// <remarks>
/// The mirror of <c>IDataStoreBuilder</c>: configuration goes in, the thing it describes comes out.
/// Choosing <em>which</em> configuration belongs to the service that reads it, not to the builder.
/// <para>
/// It uses the Sources already composed onto the configuration to construct
/// <see cref="IDataSetSource"/> and <see cref="IDataSetJoin"/> instances, infers the
/// <see cref="IDataSetCompositionType"/> from the source and join count, and assembles the graph.
/// </para>
/// </remarks>
public interface IDataSetBuilder
{
    /// <summary>Supplies the configuration to build from.</summary>
    /// <param name="dataSetConfig">The dataset's configuration.</param>
    IGenericResult Configure(DataSetConfiguration dataSetConfig);

    /// <summary>Builds the dataset the supplied configuration describes.</summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    Task<IGenericResult<IDataSet>> Build(CancellationToken cancellationToken = default);
}
