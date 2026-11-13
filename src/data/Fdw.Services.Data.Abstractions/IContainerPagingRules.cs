using System.Collections.Generic;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Abstractions;

/// <summary>
/// What a data store type says about how its containers page their rows (FDW-5562): the checks a container's own paging passes when it is saved, and the paging a
/// container reads with once it is laid over its connection's. A store type whose containers do not page has none.
/// </summary>
/// <remarks>
/// Declared on the data store type, like <see cref="IDataStoreType.AddressesContainersAsFiles"/>, so no endpoint matches on a connection type: the store type is
/// the one that knows its connection's paging style and the settings of each style.
/// </remarks>
public interface IContainerPagingRules
{
    /// <summary>Checks a container's own paging against the connection of its data store, as the read would use it.</summary>
    /// <param name="connection">The connection configuration of the container's data store.</param>
    /// <param name="containerName">The container, for the refusal.</param>
    /// <param name="style">The paging style the container states, or null for its connection's.</param>
    /// <param name="settings">The paging settings the container states.</param>
    /// <param name="logger">The logger a refusal is written to.</param>
    /// <returns>Success, which is also what a container that states no paging gets; or the refusal: a style no option answers to, a setting the style does not read, a missing or bad value, or settings with no style at either level.</returns>
    IGenericResult Check(
        IConnectionImplementationConfiguration connection, string containerName, string? style, IReadOnlyDictionary<string, string?> settings, ILogger logger);

    /// <summary>Works out the paging a container reads with.</summary>
    /// <param name="connection">The connection configuration of the container's data store.</param>
    /// <param name="containerName">The container, for the refusal.</param>
    /// <param name="style">The paging style the container states, or null for its connection's.</param>
    /// <param name="settings">The paging settings the container states.</param>
    /// <param name="logger">The logger a refusal is written to.</param>
    /// <returns>The paging with where each value came from; success with no value when neither the container nor its connection states a style, which is a connection saved before paging was stated; or the refusal <see cref="Check"/> gives.</returns>
    IGenericResult<ContainerPagingView?> Resolve(
        IConnectionImplementationConfiguration connection, string containerName, string? style, IReadOnlyDictionary<string, string?> settings, ILogger logger);
}
