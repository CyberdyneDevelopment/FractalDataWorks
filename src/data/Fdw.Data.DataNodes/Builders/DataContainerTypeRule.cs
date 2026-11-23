using System;
using System.Linq;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Connections;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Builders;

/// <summary>
/// The one rule for the type a container states (FDW-6428): it states one, the type is registered, and a store of its data store type holds it.
/// </summary>
/// <remarks>
/// Why one rule: the store's save asks it (so a container with no type, an unregistered type or another connection type's option is refused at the write, a coded 400) and every
/// store builder asks it (so a store that was written can never fail to build for this reason), where each builder used to repeat the first two checks and write its own
/// own-option test by hand. The data store type states what it holds (<see cref="IDataStoreType.ContainerTypeNames"/>); nothing here names a connection type.
/// </remarks>
public static class DataContainerTypeRule
{
    /// <summary>Checks that a container states a type and that ContainerTypes holds it.</summary>
    /// <param name="container">The container.</param>
    /// <param name="logger">The logger the refusal is written to.</param>
    /// <returns>The container's type, or a coded failure naming the container.</returns>
    public static IGenericResult<IContainerType> Stated(DataContainerConfiguration container, ILogger logger)
    {
        // A container states its type; none is assumed for it, and nothing is looked up by an empty name (FDW-6387).
        if (string.IsNullOrEmpty(container.TypeId))
            return GenericResult<IContainerType>.Failure(DataStoreLoaderLog.ContainerTypeNotStated(logger, container.Name));

        var type = ContainerTypes.ByName(container.TypeId);
        return ReferenceEquals(type, ContainerTypes.NotFound)
            ? GenericResult<IContainerType>.Failure(DataStoreLoaderLog.ContainerTypeNotRegistered(logger, container.Name, container.TypeId))
            : GenericResult<IContainerType>.Success(type);
    }

    /// <summary>Checks that a container states a registered type and that a store of the data store type holds it.</summary>
    /// <param name="container">The container.</param>
    /// <param name="storeType">The data store type of the store the container is written to or built into.</param>
    /// <param name="logger">The logger the refusal is written to.</param>
    /// <returns>The container's type, or a coded failure naming the container.</returns>
    public static IGenericResult<IContainerType> Check(DataContainerConfiguration container, IDataStoreType storeType, ILogger logger)
    {
        var stated = Stated(container, logger);
        if (stated.IsFailure || stated.Value is not { } type)
            return stated;

        return storeType.ContainerTypeNames.Contains(type.Name, StringComparer.Ordinal)
            ? stated
            : GenericResult<IContainerType>.Failure(DataStoreLoaderLog.ContainerTypeNotHeldByStoreType(
                logger, container.Name, type.Name, storeType.Name, string.Join(", ", storeType.ContainerTypeNames)));
    }
}
