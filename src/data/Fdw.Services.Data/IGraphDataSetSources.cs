using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data;

/// <summary>Reads what the check before save needs to know of the data sets and data stores a Graph data set draft reads (FDW-5290).</summary>
/// <remarks>
/// A seam over the configuration providers so the check can be proved without a store, and so a host can say what a name means. A name is
/// asked once per call however often a draft names it.
/// </remarks>
public interface IGraphDataSetSources
{
    /// <summary>Reads the named data sets.</summary>
    /// <param name="names">The data set names, without regard to case.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>One entry for every name asked, keyed without regard to case; the entry of a name that does not exist says so. Or the failure of the read.</returns>
    Task<IGenericResult<IReadOnlyDictionary<string, GraphDataSetSourceFacts>>> DataSets(
        IReadOnlyCollection<string> names, CancellationToken cancellationToken = default);

    /// <summary>Reads the stored fields of the named data sets (FDW-5522).</summary>
    /// <remarks>
    /// The fields the describer starts from when the check judges what a field out is fed by. It is the projection <c>GraphDataSetFields.Read</c> makes for the describe routes (the fields
    /// not deleted, by ordinal), one read per distinct name, and a test holds the two to the same answer.
    /// </remarks>
    /// <param name="names">The data set names, without regard to case.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>For each data set that exists, its fields in order, keyed without regard to case; a data set that does not exist is not in the answer. Or the failure of a read.</returns>
    Task<IGenericResult<IReadOnlyDictionary<string, IReadOnlyList<GraphDataSetFieldFact>>>> Fields(
        IReadOnlyCollection<string> names, CancellationToken cancellationToken = default);

    /// <summary>Finds where a data store holds the named containers (FDW-5408).</summary>
    /// <remarks>Answers from the data store's stored containers with no connection to the store, in one read for every name asked.</remarks>
    /// <param name="dataStore">The name of a data store that exists.</param>
    /// <param name="names">The container names, without regard to case.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>For each name the data store holds, the names of the paths it is held in (more than one when the data store holds that name in more than one path: the data layer makes a container name unique within a path, not within a store), keyed without regard to case; a name the data store does not hold is not in the answer. Or the failure of the read.</returns>
    Task<IGenericResult<IReadOnlyDictionary<string, IReadOnlyList<string>>>> Containers(
        string dataStore, IReadOnlyCollection<string> names, CancellationToken cancellationToken = default);

    /// <summary>Reads the stored fields of one container of a data store (FDW-5523), so the describer can say what a container source's output holds.</summary>
    /// <remarks>Answers from the data store's stored containers with no connection to the store, in one narrowed read of the data store. No container is chosen for the caller: a name held in more than one path with no path stated is a failure that says so.</remarks>
    /// <param name="component">The name of the component that reads the container, for the failure's message.</param>
    /// <param name="dataStore">The name of the data store.</param>
    /// <param name="path">The path the component states, or none.</param>
    /// <param name="container">The container's name, without regard to case.</param>
    /// <param name="logger">The logger a failure's result code is written to.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The container's fields that are not deleted, in the order the container states them; or the failure: the data store or the container is not there, the container is not in the stated path, or it is in more than one path and none is stated, or the read failed.</returns>
    Task<IGenericResult<IReadOnlyList<GraphDataSetFieldFact>>> ContainerFields(
        string component, string dataStore, string? path, string container, ILogger logger, CancellationToken cancellationToken = default);

    /// <summary>Finds which of the named data stores exist.</summary>
    /// <param name="names">The data store names, without regard to case.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The names that exist, compared without regard to case, or the failure of the read.</returns>
    Task<IGenericResult<IReadOnlySet<string>>> DataStores(
        IReadOnlyCollection<string> names, CancellationToken cancellationToken = default);
}
